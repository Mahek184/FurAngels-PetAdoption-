// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Models;
using PetAdoption.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PetAdoption.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
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

        // Login GET
        public IActionResult Login()
        {
            if (TempData["Success"] != null)
            {
                ViewBag.Success = TempData["Success"];
            }
            return View();
        }

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
                TempData["Success"] = "Login successful!";
                return RedirectToAction("Afterlogin"); // Redirects back to login page for simplicity
            }

            ViewBag.Error = "Invalid email or password";
            return View();
        }

        // Signup GET
        public IActionResult Signup()
        {
            return View();
        }

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

            TempData["Success"] = "Signup successful! Please log in.";
            return RedirectToAction("Login");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hashedPassword;
        }


    }
}