using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Data;
using VehiclePartsAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.Configure<ExternalServicesOptions>(
    builder.Configuration.GetSection("ExternalServices"));

var app = builder.Build();

// ── Database: migrate + seed ──────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        if (db.Database.CanConnect())
        {
            Console.WriteLine("✅ Database connected!");
            db.Database.Migrate();
            Console.WriteLine("✅ Migrations applied.");

            // Seed first Admin if none exist
            if (!await db.Staff.AnyAsync())
            {
                db.Staff.Add(new Staff
                {
                    FirstName    = "Super",
                    LastName     = "Admin",
                    Email        = "admin@vehicleparts.com",
                    Phone        = "9800000000",
                    Role         = "Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    IsActive     = true
                });
                await db.SaveChangesAsync();
                Console.WriteLine("✅ Default admin seeded → admin@vehicleparts.com / admin123");
            }
        }
        else
        {
            Console.WriteLine("❌ Database connection failed!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ DB error: {ex.Message}");
    }
}

// ── Middleware ────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAllOrigins");
app.UseAuthorization();
app.MapControllers();
app.Run();
