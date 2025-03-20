// Services/EmailService.cs
using MailKit.Net.Smtp; // Correct namespace for MailKit's SmtpClient
using MimeKit;          // For MimeMessage, MailboxAddress, BodyBuilder
using Microsoft.Extensions.Options;
using PetAdoption.Models;

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
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient()) // MailKit.Net.Smtp.SmtpClient
            {
                try
                {
                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                    await client.SendAsync(message);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to send email: {ex.Message}", ex);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}