using Microsoft.EntityFrameworkCore;
using PetAdoption.Models;

namespace PetAdoption.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<OtpRecord> OtpRecords { get; set; }
    }
}