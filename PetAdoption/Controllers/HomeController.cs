// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Data;
using PetAdoption.Models;
using PetAdoption.Services;
using System.Security.Cryptography;
using System.Text;

namespace PetAdoption.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, EmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public IActionResult Index() => View();
        public IActionResult Privacy() => View();
        public IActionResult GroomingPage() => View();
        public IActionResult AboutUs() => View();
        public IActionResult PetShop() => View();
        public IActionResult Consult() => View();
        public IActionResult Consultform() => View();
        public IActionResult Vetconsultform() => View();
        public IActionResult PetAbuse() => View();
        public IActionResult Training() => View();
        public IActionResult Afterlogin() => View();
        public IActionResult Adminlogin() => View();

        // GET: /Home/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Home/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please provide a valid email address.";
                return View();
            }

            // Log the email being searched
            _logger.LogInformation("Searching for email: {Email}", email);

            // Check if user exists (case-insensitive and trimmed)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == email.Trim().ToLower());

            if (user == null)
            {
                // Security: Don't reveal if email exists
                _logger.LogInformation("No user found for email: {Email}", email);
                ViewBag.Success = "If an account exists, an OTP has been sent to your email."; // Generic message
                return View();
            }

            // Generate OTP
            string otp = GenerateOtp();
            _logger.LogInformation("Generated OTP: {Otp} for email: {Email}", otp, email);

            // Save OTP to database
            var otpRecord = new OtpRecord
            {
                Email = email,
                Otp = otp,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };
            _context.OtpRecords.Add(otpRecord);
            await _context.SaveChangesAsync();

            // Send OTP via email
            try
            {
                string subject = "Fur Angels - OTP for Password Reset";
                string body = $"Your OTP for password reset is: <strong>{otp}</strong>. It is valid for 10 minutes.";
                await _emailService.SendEmailAsync(email, subject, body);
                _logger.LogInformation("OTP sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP to {Email}", email);
                ViewBag.Error = "Failed to send OTP. Please try again later.";
                return View();
            }

            // Store email in session for the reset flow
            HttpContext.Session.SetString("ResetEmail", email);

            // Redirect to OTP verification
            return RedirectToAction("VerifyOtp", new { email = email });
        }

        // GET: /Home/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            if (string.IsNullOrEmpty(email) || HttpContext.Session.GetString("ResetEmail") != email)
            {
                return RedirectToAction("ForgotPassword");
            }
            ViewBag.Email = email;
            return View();
        }

        // POST: /Home/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string email, string otp)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
            {
                ViewBag.Error = "Email and OTP are required.";
                ViewBag.Email = email;
                return View();
            }

            if (HttpContext.Session.GetString("ResetEmail") != email)
            {
                return RedirectToAction("ForgotPassword");
            }

            var otpRecord = await _context.OtpRecords
                .FirstOrDefaultAsync(o => o.Email == email && o.Otp == otp && !o.IsUsed && o.CreatedAt > DateTime.UtcNow.AddMinutes(-10));

            if (otpRecord == null)
            {
                _logger.LogWarning("Invalid or expired OTP for email: {Email}, OTP: {Otp}", email, otp);
                ViewBag.Error = "Invalid or expired OTP.";
                ViewBag.Email = email;
                return View();
            }

            // Mark OTP as used
            otpRecord.IsUsed = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("OTP verified for email: {Email}", email);

            return RedirectToAction("ResetPassword", new { email = email });
        }

        // GET: /Home/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email) || HttpContext.Session.GetString("ResetEmail") != email)
            {
                return RedirectToAction("ForgotPassword");
            }
            ViewBag.Email = email;
            return View();
        }

        // POST: /Home/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "All fields are required.";
                ViewBag.Email = email;
                return View();
            }

            if (HttpContext.Session.GetString("ResetEmail") != email)
            {
                return RedirectToAction("ForgotPassword");
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                ViewBag.Email = email;
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                _logger.LogError("User not found during reset for email: {Email}", email);
                ViewBag.Error = "User not found.";
                ViewBag.Email = email;
                return View();
            }

            // Update password with hashing
            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset successfully for email: {Email}", email);

            // Clear session
            HttpContext.Session.Remove("ResetEmail");

            TempData["Success"] = "Password has been reset successfully.";
            return RedirectToAction("Login");
        }

        // GET: /Home/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (TempData["Success"] != null)
            {
                ViewBag.Success = TempData["Success"];
            }
            return View();
        }

        // POST: /Home/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter both email and password";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user != null && VerifyPassword(password, user.PasswordHash))
            {
                HttpContext.Session.SetString("UserEmail", user.Email);
                _logger.LogInformation("User logged in: {Email}", email);
                TempData["Success"] = "Login successful!";
                return RedirectToAction("Afterlogin");
            }

            _logger.LogWarning("Login failed for email: {Email}", email);
            ViewBag.Error = "Invalid email or password";
            return View();
        }

        // GET: /Home/Signup
        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        // POST: /Home/Signup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Signup(string fullName, string email, string mobile, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(mobile) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "All fields are required";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match";
                return View();
            }

            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "Email already exists";
                return View();
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                MobileNumber = mobile,
                PasswordHash = HashPassword(password)
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            _logger.LogInformation("User signed up: {Email}", email);

            TempData["Success"] = "Signup successful! Please log in.";
            return RedirectToAction("Login");
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            _logger.LogInformation("User logged out");
            return RedirectToAction("Login");
        }

        // Helper method to hash password
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Helper method to verify password
        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hashedPassword;
        }

        // Helper method to generate OTP
        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); // 6-digit OTP
        }
    }
}