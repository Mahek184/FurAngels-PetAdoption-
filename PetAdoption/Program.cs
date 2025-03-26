using Microsoft.EntityFrameworkCore;
using PetAdoption.Data;
using PetAdoption.Models;
using PetAdoption.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register DbContext for Users and OtpRecords
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register DbContext for Admins
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AdminConnection")));

// Register EmailSettings and EmailService
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<EmailService>();

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Seed the AdminDbContext database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var adminContext = services.GetRequiredService<AdminDbContext>();
        Console.WriteLine("AdminDbContext connection string: " + adminContext.Database.GetConnectionString());
        adminContext.Database.Migrate(); // Apply migrations

        // Clear existing data
        adminContext.Admins.RemoveRange(adminContext.Admins);
        adminContext.SaveChanges();

        // Seed the admin user
        var admin = new Admin
        {
            Email = "mahekbabariya18@gmail.com",
            Password = "mahek@123" // In production, hash this
        };
        adminContext.Admins.Add(admin);
        adminContext.SaveChanges();
        Console.WriteLine("Admin user seeded successfully with Email: mahekbabariya18@gmail.com, Password: mahek@123");

        // Verify the data
        var admins = adminContext.Admins.ToList();
        if (admins.Any())
        {
            Console.WriteLine("Database contents after seeding:");
            foreach (var a in admins)
            {
                Console.WriteLine($"ID: {a.Id}, Email: {a.Email}, Password: {a.Password}");
            }
        }
        else
        {
            Console.WriteLine("No data found in Admins table after seeding!");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the AdminDbContext: {Message}", ex.Message);
        throw;
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();