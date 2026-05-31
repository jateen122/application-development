using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Data;
using VehiclePartsAPI.Models;


public static class DatabaseSeeder
{
    public static async Task SeedAdminAsync(AppDbContext db)
    {

        const string adminEmail     = "jatinshrestha2022@gmail.com";
        const string adminPassword  = "admin123";
        const string adminFirstName = "System";
        const string adminLastName  = "Admin";
        const string adminPhone     = "9800000000";


        var exists = await db.Staff.AnyAsync(s => s.Role == "Admin");
        if (exists)
        {
            Console.WriteLine(" Admin already exists — skipping seed.");
            return;
        }

        var admin = new Staff
        {
            FirstName    = adminFirstName,
            LastName     = adminLastName,
            Email        = adminEmail,
            Phone        = adminPhone,
            Role         = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        db.Staff.Add(admin);
        await db.SaveChangesAsync();

        Console.WriteLine(" Admin account created!");
        Console.WriteLine($"   Email:    {adminEmail}");
        Console.WriteLine($"   Password: {adminPassword}");
        Console.WriteLine("     Change the password after first login.");
    }
}
