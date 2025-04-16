using Microsoft.AspNetCore.Mvc;
using PetAdoption.Data;
using PetAdoption.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using BCrypt.Net;
using Org.BouncyCastle.Crypto.Generators;

namespace PetAdoption.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminDbContext _context;

        public AdminController(AdminDbContext context)
        {
            _context = context;
        }

        // GET: Admin Login
        public IActionResult AdminLogin()
        {
            return View();
        }

        // POST: Admin Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminLogin(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            var admin = _context.Admins.FirstOrDefault(a => a.Email == email);
            if (admin != null && BCrypt.Net.BCrypt.Verify(password, admin.Password))
            {
                HttpContext.Session.SetString("AdminEmail", admin.Email);
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        // GET: Admin Dashboard
        public IActionResult Dashboard()
        {
            var vetConsultations = _context.VetConsultations.ToList();
            return View(vetConsultations);
        }

        // GET: Vet Consultation Table
        public IActionResult VetConsult()
        {
            var vetConsultations = _context.VetConsultations.ToList();
            return View(vetConsultations);
        }

        // GET: Vet Consultation Form
        public IActionResult SubmitVetConsultation()
        {
            return View("~/Views/VetConsultation/SubmitVetConsultation.cshtml", new VetConsultation());
        }

        // POST: Vet Consultation Form Submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitVetConsultation(VetConsultation model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.CreatedAt = DateTime.Now;
                    _context.VetConsultations.Add(model);
                    _context.SaveChanges();
                    TempData["Success"] = "Vet consultation request submitted successfully!";
                    return RedirectToAction("VetConsult");
                }
                catch (Exception)
                {
                    TempData["Error"] = "An error occurred while saving the request. Please try again.";
                }
            }
            else
            {
                TempData["Error"] = "Please fill all required fields correctly.";
            }

            return View("~/Views/VetConsultation/SubmitVetConsultation.cshtml", model);
        }

        // GET: Add Shop (Placeholder)
        public IActionResult Addshop()
        {
            return View();
        }

        // GET: Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("AdminLogin");
        }
    }
}