using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Data;
using PetAdoption.Models;
using PetAdoption.Services;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
        public IActionResult DogTraining() => View();
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
        public async Task<IActionResult> ForgotPassword(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please provide a valid email address.";
                return View();
            }

            _logger.LogInformation("Searching for email: {Email}", email);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.Trim().ToLower() == email.Trim().ToLower());

            if (user == null)
            {
                _logger.LogInformation("No user found for email: {Email}", email);
                ViewBag.Success = "If an account exists, an OTP has been sent to your email.";
                return View();
            }

            string otp = GenerateOtp();
            _logger.LogInformation("Generated OTP: {Otp} for email: {Email}", otp, email);

            var otpRecord = new OtpRecord
            {
                Email = email,
                Otp = otp,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };
            _context.OtpRecords.Add(otpRecord);
            await _context.SaveChangesAsync();

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

            HttpContext.Session.SetString("ResetEmail", email);
            return RedirectToAction("VerifyOtp", new { email });
        }

        // GET: /Home/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp(string? email)
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
        public async Task<IActionResult> VerifyOtp(string? email, string? otp)
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

            otpRecord.IsUsed = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("OTP verified for email: {Email}", email);

            return RedirectToAction("ResetPassword", new { email });
        }

        // GET: /Home/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string? email)
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
        public async Task<IActionResult> ResetPassword(string? email, string? newPassword, string? confirmPassword)
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

            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset successfully for email: {Email}", email);

            HttpContext.Session.Remove("ResetEmail");

            TempData["Success"] = "Password has been reset successfully.";
            return RedirectToAction("Login");
        }

        // GET: /Home/Login
        [HttpGet]
        public IActionResult Login(string? productName = null, string? productPrice = null, string? returnUrl = null)
        {
            if (TempData["Success"] != null)
            {
                ViewBag.Success = TempData["Success"];
            }
            ViewBag.ProductName = productName;
            ViewBag.ProductPrice = productPrice;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Home/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string? email, string? password, string? productName = null, string? productPrice = null, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter both email and password";
                ViewBag.ProductName = productName;
                ViewBag.ProductPrice = productPrice;
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null && VerifyPassword(password, user.PasswordHash))
            {
                HttpContext.Session.SetString("UserEmail", user.Email);
                _logger.LogInformation("User logged in: {Email}", email);

                // If product details are provided, add to cart
                if (!string.IsNullOrEmpty(productName) && !string.IsNullOrEmpty(productPrice))
                {
                    var imageMap = new Dictionary<string, string>
                    {
                        { "TheFurAngels X Rubber Spiked hard ball", "/image/DT1.jpg" },
                        { "TheFurAngels XDogily Dassic Dog Sofa", "/image/DT5.jpg" },
                        { "TheFurAngels XDogily Classic Dog Sofa", "/image/DF4.jpg" },
                        { "TheFurAngels XDogily Racoa Dog Sofa", "/image/DF6.jpg" },
                        { "TheFurAngels XDogily Luxury Bed", "/image/DF3.jpg" },
                        { "TheFurAngels XDogily Premium Sofa", "/image/dog-bed5.jpg" },
                        { "TheFurAngels XDogily Modern Sofa", "/image/dog-bed6.jpg" },
                        { "TheFurAngels X Harness For Dogs", "/image/DA2.PNG" },
                        { "TheFurAngels XDog Leash", "/image/DA6.jpg" },
                        { "TheFurAngels XDogily Classic Dog Collar", "/image/DA3.PNG" }
                    };
                    var imageUrl = imageMap.ContainsKey(productName) ? imageMap[productName] : "/image/default.jpg";

                    var existingItem = await _context.CartItems
                        .FirstOrDefaultAsync(c => c.UserEmail == email && c.ProductName == productName);
                    if (existingItem != null)
                    {
                        existingItem.Quantity += 1;
                    }
                    else
                    {
                        var cartItem = new CartItem
                        {
                            UserEmail = email,
                            ProductName = productName,
                            ProductPrice = productPrice,
                            ImageUrl = imageUrl,
                            Quantity = 1,
                            AddedAt = DateTime.UtcNow
                        };
                        _context.CartItems.Add(cartItem);
                    }
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Added {ProductName} to cart for {Email}", productName, email);
                }

                TempData["Success"] = "Login successful!";
                return Redirect(returnUrl ?? "/Home/Afterlogin");
            }

            _logger.LogWarning("Login failed for email: {Email}", email);
            ViewBag.Error = "Invalid email or password";
            ViewBag.ProductName = productName;
            ViewBag.ProductPrice = productPrice;
            ViewBag.ReturnUrl = returnUrl;
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
        public IActionResult Signup(string? fullName, string? email, string? mobile, string? password, string? confirmPassword)
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

        // Check Login Status
        [HttpGet]
        public IActionResult CheckLoginStatus()
        {
            bool isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail"));
            return Json(new { isLoggedIn });
        }

        // Add to Cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromBody] CartItemDto? cartItemDto)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, redirect = Url.Action("Login", new { productName = cartItemDto?.ProductName, productPrice = cartItemDto?.ProductPrice, returnUrl = "/Home/PetShop" }) });
            }

            if (cartItemDto == null || string.IsNullOrEmpty(cartItemDto.ProductName) || string.IsNullOrEmpty(cartItemDto.ProductPrice))
            {
                return Json(new { success = false, message = "Invalid product details" });
            }

            var imageMap = new Dictionary<string, string>
            {
                { "TheFurAngels X Rubber Spiked hard ball", "https://via.placeholder.com/80" },
                { "TheFurAngels XDogily Dassic Dog Sofa", "https://via.placeholder.com/80" },
                { "TheFurAngels XDogily Classic Dog Sofa", "https://via.placeholder.com/80" },
                { "TheFurAngels XDogily Racoa Dog Sofa", "https://via.placeholder.com/80" },
                { "TheFurAngels XDogily Luxury Bed", "https://via.placeholder.com/80" },
                { "TheFurAngels XDogily Premium Sofa", "https://via.placeholder.com/80" },
                { "TheFurAngels XDogily Modern Sofa", "https://via.placeholder.com/80" },
                { "TheFurAngels X Harness For Dogs", "https://via.placeholder.com/80" },
                { "TheFurAngels XDog Leash", "https://via.placeholder.com/80" },
                { "TheFurAngels XDogily Classic Dog Collar", "https://via.placeholder.com/80" }
            };
            var imageUrl = imageMap.ContainsKey(cartItemDto.ProductName) ? imageMap[cartItemDto.ProductName] : "https://via.placeholder.com/80";

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserEmail == userEmail && c.ProductName == cartItemDto.ProductName);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserEmail = userEmail,
                    ProductName = cartItemDto.ProductName,
                    ProductPrice = cartItemDto.ProductPrice,
                    ImageUrl = imageUrl,
                    Quantity = 1,
                    AddedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Added {ProductName} to cart for {Email}", cartItemDto.ProductName, userEmail);

            return Json(new { success = true, message = "Item added to cart" });
        }

        // View Cart
        [HttpGet]
        public async Task<IActionResult> CartItem()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            var cartItems = await _context.CartItems
                .Where(c => c.UserEmail == userEmail)
                .ToListAsync();
            return View(cartItems);
        }

        // Update Quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, redirect = Url.Action("Login") });
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserEmail == userEmail);
            if (cartItem == null)
            {
                return Json(new { success = false, message = "Item not found" });
            }

            if (quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = quantity;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Quantity updated" });
        }

        // Delete Item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, redirect = Url.Action("Login") });
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserEmail == userEmail);
            if (cartItem == null)
            {
                return Json(new { success = false, message = "Item not found" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Item removed" });
        }

        // Submit Vet Consultation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVetConsultation(VetConsultation model)
        {
            if (!ModelState.IsValid)
            {
                return View("Consultform", model);
            }

            model.CreatedAt = DateTime.UtcNow;
            _context.VetConsultations.Add(model);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Vet consultation request saved for {OwnerEmail}", model.OwnerEmail);

            string subject = "Fur Angels - Vet Consultation Request";
            string body = $@"
                <h2>Vet Consultation Request</h2>
                <p><strong>Pet's Name:</strong> {model.PetName}</p>
                <p><strong>Pet Type:</strong> {model.PetType}</p>
                <p><strong>Pet's Age:</strong> {model.PetAge} years</p>
                <p><strong>Owner's Name:</strong> {model.OwnerName}</p>
                <p><strong>Owner's Email:</strong> {model.OwnerEmail}</p>
                <p><strong>Owner's Phone:</strong> {model.OwnerPhone}</p>
                <p><strong>Concern:</strong> {model.Concern}</p>
                <p><strong>Preferred Date:</strong> {model.PreferredDate.ToString("d MMMM yyyy")}</p>
                <p><strong>Submitted At:</strong> {model.CreatedAt.ToString("d MMMM yyyy HH:mm")}</p>
                <p>We will contact you soon to confirm your consultation.</p>
            ";

            try
            {
                await _emailService.SendEmailAsync(model.OwnerEmail, subject, body);
                await _emailService.SendEmailAsync("mahekbabariya18@gmail.com", subject, body);
                _logger.LogInformation("Vet consultation email sent to {OwnerEmail} and admin", model.OwnerEmail);
                TempData["Success"] = "Your vet consultation request has been submitted successfully! You will receive a confirmation email shortly.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send vet consultation email to {OwnerEmail}", model.OwnerEmail);
                TempData["Error"] = "Your request was saved, but we failed to send the confirmation email. Please contact support.";
            }

            return RedirectToAction("Consultform");
        }

        // Submit Dog Training Booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDogTrainingBooking(DogTrainingBooking model)
        {
            if (!ModelState.IsValid)
            {
                return View("DogTraining", model);
            }

            model.CreatedAt = DateTime.UtcNow;
            _context.DogTrainingBookings.Add(model);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Dog training booking request saved for location {Location}", model.Location);

            string subject = "Fur Angels - Dog Training Booking Request";
            string trainingTypes = "";
            if (model.PuppyTraining) trainingTypes += "Puppy Training, ";
            if (model.BasicObedienceTraining) trainingTypes += "Basic Obedience Training, ";
            if (model.BehaviorCorrectionTraining) trainingTypes += "Behavior Correction Training, ";
            trainingTypes = trainingTypes.TrimEnd(',', ' ');

            string body = $@"
                <h2>Dog Training Booking Request</h2>
                <p><strong>Training Types:</strong> {trainingTypes}</p>
                <p><strong>Location:</strong> {model.Location}</p>
                <p><strong>Dog's Breed:</strong> {model.DogBreed}</p>
                <p><strong>Submitted At:</strong> {model.CreatedAt.ToString("d MMMM yyyy HH:mm")}</p>
                <p>We will contact you soon to confirm your dog training session.</p>
            ";

            try
            {
                await _emailService.SendEmailAsync("mahekbabariya18@gmail.com", subject, body);
                _logger.LogInformation("Dog training booking email sent to admin");
                TempData["Success"] = "Your dog training booking request has been submitted successfully! We will contact you soon to confirm your session.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send dog training booking email");
                TempData["Error"] = "Your request was saved, but we failed to send the confirmation email. Please contact support.";
            }

            return RedirectToAction("DogTraining");
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

    // DTO for JSON deserialization
    public class CartItemDto
    {
        public string? UserEmail { get; set; }
        public string? ProductName { get; set; }
        public string? ProductPrice { get; set; }
    }
}