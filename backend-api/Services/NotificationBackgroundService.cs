using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Services;

namespace VehiclePartsAPI.BackgroundServices
{
    /// <summary>
    /// Feature 15 — Runs on a background timer.
    /// Every hour:
    ///   • Checks for parts with StockQty less than 10 and emails the first active Admin.
    ///   • Checks for Credit invoices older than 30 days and emails each customer a reminder.
    /// </summary>
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationBackgroundService> _logger;

        // How often to run checks (1 hour in production; easy to change for testing)
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        public NotificationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationBackgroundService started.");

            // Run immediately on startup, then repeat on Interval
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunChecksAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in NotificationBackgroundService.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task RunChecksAsync(CancellationToken ct)
        {
            using var scope       = _scopeFactory.CreateScope();
            var db                = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService      = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // ── 1. Low stock alert ────────────────────────────────
            var lowStockParts = await db.Parts
                .Where(p => p.StockQty < 10)
                .OrderBy(p => p.StockQty)
                .ToListAsync(ct);

            if (lowStockParts.Any())
            {
                var adminEmail = await db.Staff
                    .Where(s => s.Role == "Admin" && s.IsActive)
                    .Select(s => s.Email)
                    .FirstOrDefaultAsync(ct);

                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    _logger.LogInformation(
                        "Sending low-stock alert: {Count} parts below threshold to {Admin}.",
                        lowStockParts.Count, adminEmail);
                    await emailService.SendLowStockAlertAsync(lowStockParts, adminEmail);
                }
            }

            // ── 2. Overdue credit reminders (> 30 days) ───────────
            var cutoff = DateTime.UtcNow.AddDays(-30);

            var customersWithOverdue = await db.Customers
                .Include(c => c.SaleInvoices)
                .Where(c => c.SaleInvoices.Any(si => si.Status == "Credit" && si.InvoiceDate <= cutoff))
                .ToListAsync(ct);

            foreach (var customer in customersWithOverdue)
            {
                var overdueInvoices = customer.SaleInvoices
                    .Where(si => si.Status == "Credit" && si.InvoiceDate <= cutoff)
                    .ToList();

                _logger.LogInformation(
                    "Sending overdue credit reminder to {Email}: {Count} invoice(s).",
                    customer.Email, overdueInvoices.Count);

                await emailService.SendOverdueCreditReminderAsync(customer, overdueInvoices);
            }
        }
    }
}
