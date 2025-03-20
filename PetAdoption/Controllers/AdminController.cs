using Microsoft.AspNetCore.Mvc;
using PetAdoption.Data;
using PetAdoption.Models;
using System.Linq;

namespace PetAdoption.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminDbContext _context;

        public AdminController(AdminDbContext context)
        {
            _context = context;
        }

        public IActionResult AdminLogin()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }
        public IActionResult Addshop()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminLogin(string email, string password)
        {
            Console.WriteLine($"Login attempt - Email: '{email}', Password: '{password}'");
            var admin = _context.Admins
                .FirstOrDefault(a => a.Email == email && a.Password == password);

            if (admin != null)
            {
                Console.WriteLine($"Login successful! Found admin - Email: {admin.Email}, Password: {admin.Password}");
                ViewBag.Success = "Login successful!";
                return RedirectToAction("dashboard", "Admin");
            }
            else
            {
                Console.WriteLine("Login failed: No matching admin found in database.");
                var allAdmins = _context.Admins.ToList();
                if (allAdmins.Any())
                {
                    Console.WriteLine("Current database contents:");
                    foreach (var a in allAdmins)
                    {
                        Console.WriteLine($"ID: {a.Id}, Email: {a.Email}, Password: {a.Password}");
                    }
                }
                else
                {
                    Console.WriteLine("Admins table is empty!");
                }
                ViewBag.Error = "Invalid email or password";
                return View();
            }
        }
    }
}