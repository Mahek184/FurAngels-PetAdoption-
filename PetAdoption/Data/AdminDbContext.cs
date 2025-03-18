﻿using Microsoft.EntityFrameworkCore;
using PetAdoption.Models;

namespace PetAdoption.Data
{
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options)
            : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }
    }
}