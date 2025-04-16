using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using PetAdoption.Models;
using System;
using System.Threading.Tasks;

namespace PetAdoption.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException("Recipient email cannot be empty.", nameof(toEmail));
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            // Retry logic for transient failures
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                using (var client = new SmtpClient())
                {
                    try
                    {
                        Console.WriteLine($"Attempt {attempt}: Connecting to SMTP {_emailSettings.SmtpServer}:{_emailSettings.SmtpPort}");
                        await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);

                        Console.WriteLine("Authenticating...");
                        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                        Console.WriteLine("Sending email...");
                        await client.SendAsync(message);

                        Console.WriteLine("Email sent successfully.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
                        if (attempt == 3)
                        {
                            throw new Exception($"Failed to send email after {attempt} attempts: {ex.Message}", ex);
                        }
                        await Task.Delay(1000 * attempt); // Exponential backoff
                    }
                    finally
                    {
                        if (client.IsConnected)
                        {
                            await client.DisconnectAsync(true);
                        }
                    }
                }
            }
        }
    }
}