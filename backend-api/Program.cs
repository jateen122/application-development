using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Data;
using VehiclePartsAPI.Models;
using VehiclePartsAPI.Services;
using VehiclePartsAPI.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS — allow all for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// External services config (SMTP, Khalti key, etc.)
builder.Services.Configure<ExternalServicesOptions>(
    builder.Configuration.GetSection("ExternalServices"));

// Email service
builder.Services.AddScoped<IEmailService, EmailService>();

// HttpClient for Khalti API calls
builder.Services.AddHttpClient();

// Background notification service (low stock + overdue credits)
builder.Services.AddHostedService<NotificationBackgroundService>();

var app = builder.Build();

// ── Database migrate + optional seed ─────────────────────────
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

            if (args.Contains("--seed"))
                await DatabaseSeeder.SeedAdminAsync(db);
        }
        else
        {
            Console.WriteLine("❌ Database connection failed! Check appsettings.json → ConnectionStrings.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ DB error: {ex.Message}");
    }
}

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
