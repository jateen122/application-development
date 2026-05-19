using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Data;
using VehiclePartsAPI.Models;

/// <summary>
/// Run this once to create the first Admin account.
/// Usage: dotnet run --seed
/// Edit the email/password below before running.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAdminAsync(AppDbContext db)
    {
        // ── Change these before running ──────────────────────
        const string adminEmail     = "jatinshrestha2022@gmail.com";
        const string adminPassword  = "admin123";
        const string adminFirstName = "System";
        const string adminLastName  = "Admin";
        const string adminPhone     = "9800000000";
        // ─────────────────────────────────────────────────────

        // Check if any admin already exists
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

        Console.WriteLine("✅ Admin account created!");
        Console.WriteLine($"   Email:    {adminEmail}");
        Console.WriteLine($"   Password: {adminPassword}");
        Console.WriteLine("   ⚠  Change the password after first login.");
    }
}
