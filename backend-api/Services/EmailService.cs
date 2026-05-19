using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using VehiclePartsAPI.Models;

namespace VehiclePartsAPI.Services
{
    /// <summary>
    /// Handles all outbound email: invoice delivery, low-stock alerts, overdue-credit reminders.
    /// Configured via appsettings.json → ExternalServices (SmtpServer, SmtpPort, EmailFrom, EmailPassword).
    /// Falls back gracefully when SMTP is not configured (logs to console instead).
    /// </summary>
    public interface IEmailService
    {
        Task SendSaleInvoiceAsync(SaleInvoice invoice);
        Task SendLowStockAlertAsync(List<Part> lowStockParts, string adminEmail);
        Task SendOverdueCreditReminderAsync(Customer customer, List<SaleInvoice> overdueInvoices);
    }

    public class EmailService : IEmailService
    {
        private readonly ExternalServicesOptions _opts;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<ExternalServicesOptions> opts, ILogger<EmailService> logger)
        {
            _opts   = opts.Value;
            _logger = logger;
        }

        // ── Sale Invoice Email ────────────────────────────────────
        public async Task SendSaleInvoiceAsync(SaleInvoice invoice)
        {
            var to      = invoice.Customer?.Email;
            var subject = $"Your Invoice {invoice.InvoiceNumber} — VehicleParts Pro";

            var rows = invoice.Items?.Select(i =>
                $@"<tr>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee'>{i.Part?.Name ?? "Part"}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;text-align:center'>{i.Quantity}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;text-align:right'>NPR {i.UnitPrice:N2}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;text-align:right'>NPR {i.Quantity * i.UnitPrice:N2}</td>
                   </tr>") ?? [];

            var discountRow = invoice.LoyaltyDiscountApplied
                ? $"<tr><td colspan='3' style='padding:6px 12px;color:#18a558'>Loyalty Discount (10%)</td><td style='padding:6px 12px;text-align:right;color:#18a558'>-NPR {invoice.DiscountAmount:N2}</td></tr>"
                : "";

            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'></head>
<body style='font-family:DM Sans,sans-serif;background:#f4f5f7;padding:32px'>
  <div style='max-width:560px;margin:auto;background:#fff;border-radius:14px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08)'>
    <div style='background:#2655e8;padding:24px 28px'>
      <h1 style='color:#fff;margin:0;font-size:22px'>⚙ VehicleParts<span style='opacity:0.75'>Pro</span></h1>
      <p style='color:rgba(255,255,255,0.8);margin:6px 0 0;font-size:13px'>Sales Invoice</p>
    </div>
    <div style='padding:28px'>
      <p style='color:#454c5e;margin:0 0 4px'>Dear <strong>{invoice.Customer?.FirstName} {invoice.Customer?.LastName}</strong>,</p>
      <p style='color:#8b91a5;font-size:13px'>Thank you for your purchase. Here is your invoice summary.</p>

      <div style='background:#f4f5f7;border-radius:8px;padding:14px 16px;margin:20px 0;font-size:13px'>
        <div style='display:flex;justify-content:space-between;margin-bottom:6px'>
          <span style='color:#8b91a5'>Invoice #</span><strong>{invoice.InvoiceNumber}</strong>
        </div>
        <div style='display:flex;justify-content:space-between;margin-bottom:6px'>
          <span style='color:#8b91a5'>Date</span><span>{invoice.InvoiceDate:dd MMM yyyy}</span>
        </div>
        <div style='display:flex;justify-content:space-between'>
          <span style='color:#8b91a5'>Payment</span><span>{invoice.PaymentMethod}</span>
        </div>
      </div>

      <table style='width:100%;border-collapse:collapse;font-size:13px'>
        <thead>
          <tr style='background:#f4f5f7'>
            <th style='padding:8px 12px;text-align:left;color:#8b91a5;font-weight:600'>Part</th>
            <th style='padding:8px 12px;text-align:center;color:#8b91a5;font-weight:600'>Qty</th>
            <th style='padding:8px 12px;text-align:right;color:#8b91a5;font-weight:600'>Unit</th>
            <th style='padding:8px 12px;text-align:right;color:#8b91a5;font-weight:600'>Total</th>
          </tr>
        </thead>
        <tbody>
          {string.Join("", rows)}
          <tr><td colspan='3' style='padding:8px 12px;text-align:right;color:#454c5e'>Subtotal</td><td style='padding:8px 12px;text-align:right'>NPR {invoice.SubTotal:N2}</td></tr>
          {discountRow}
          <tr style='background:#2655e8'>
            <td colspan='3' style='padding:10px 12px;color:#fff;font-weight:700'>TOTAL</td>
            <td style='padding:10px 12px;color:#fff;font-weight:700;text-align:right'>NPR {invoice.TotalAmount:N2}</td>
          </tr>
        </tbody>
      </table>

      <p style='color:#8b91a5;font-size:12px;margin-top:24px'>
        If you have any questions, please contact us. Thank you for choosing VehicleParts Pro!
      </p>
    </div>
  </div>
</body>
</html>";

            await SendAsync(to, subject, body);
        }

        // ── Low Stock Alert ───────────────────────────────────────
        public async Task SendLowStockAlertAsync(List<Part> lowStockParts, string adminEmail)
        {
            var subject = $"⚠ Low Stock Alert — {lowStockParts.Count} part(s) need restocking";

            var rows = lowStockParts.Select(p =>
                $@"<tr>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee'>{p.Name}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee'>{p.PartNumber}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee'>{p.Category}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;color:{(p.StockQty <= 3 ? "#e03c3c" : "#d97706")};font-weight:700'>{p.StockQty}</td>
                   </tr>");

            var body = $@"
<!DOCTYPE html>
<html>
<body style='font-family:DM Sans,sans-serif;background:#f4f5f7;padding:32px'>
  <div style='max-width:560px;margin:auto;background:#fff;border-radius:14px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08)'>
    <div style='background:#d97706;padding:24px 28px'>
      <h1 style='color:#fff;margin:0;font-size:20px'>⚠ Low Stock Alert</h1>
      <p style='color:rgba(255,255,255,0.85);margin:4px 0 0;font-size:13px'>VehicleParts Pro — Inventory Warning</p>
    </div>
    <div style='padding:28px'>
      <p style='color:#454c5e'>{lowStockParts.Count} part(s) have fallen below the minimum stock level of <strong>10 units</strong>. Please arrange restocking.</p>
      <table style='width:100%;border-collapse:collapse;font-size:13px;margin-top:16px'>
        <thead>
          <tr style='background:#f4f5f7'>
            <th style='padding:8px 12px;text-align:left'>Part Name</th>
            <th style='padding:8px 12px;text-align:left'>Part #</th>
            <th style='padding:8px 12px;text-align:left'>Category</th>
            <th style='padding:8px 12px;text-align:left'>Stock</th>
          </tr>
        </thead>
        <tbody>{string.Join("", rows)}</tbody>
      </table>
    </div>
  </div>
</body>
</html>";

            await SendAsync(adminEmail, subject, body);
        }

        // ── Overdue Credit Reminder ───────────────────────────────
        public async Task SendOverdueCreditReminderAsync(Customer customer, List<SaleInvoice> overdueInvoices)
        {
            var to      = customer.Email;
            var subject = "Payment Reminder — Outstanding Credit Balance | VehicleParts Pro";
            var total   = overdueInvoices.Sum(i => i.TotalAmount);

            var rows = overdueInvoices.Select(i =>
                $@"<tr>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee'>{i.InvoiceNumber}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee'>{i.InvoiceDate:dd MMM yyyy}</td>
                    <td style='padding:8px 12px;border-bottom:1px solid #eee;text-align:right;color:#e03c3c;font-weight:600'>NPR {i.TotalAmount:N2}</td>
                   </tr>");

            var body = $@"
<!DOCTYPE html>
<html>
<body style='font-family:DM Sans,sans-serif;background:#f4f5f7;padding:32px'>
  <div style='max-width:560px;margin:auto;background:#fff;border-radius:14px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08)'>
    <div style='background:#e03c3c;padding:24px 28px'>
      <h1 style='color:#fff;margin:0;font-size:20px'>Payment Reminder</h1>
      <p style='color:rgba(255,255,255,0.85);margin:4px 0 0;font-size:13px'>VehicleParts Pro</p>
    </div>
    <div style='padding:28px'>
      <p style='color:#454c5e'>Dear <strong>{customer.FirstName} {customer.LastName}</strong>,</p>
      <p style='color:#8b91a5;font-size:13px'>This is a friendly reminder that the following invoices remain unpaid for more than 30 days. Please arrange payment at your earliest convenience.</p>

      <table style='width:100%;border-collapse:collapse;font-size:13px;margin:16px 0'>
        <thead>
          <tr style='background:#f4f5f7'>
            <th style='padding:8px 12px;text-align:left'>Invoice #</th>
            <th style='padding:8px 12px;text-align:left'>Date</th>
            <th style='padding:8px 12px;text-align:right'>Amount</th>
          </tr>
        </thead>
        <tbody>
          {string.Join("", rows)}
          <tr style='background:#e03c3c'>
            <td colspan='2' style='padding:10px 12px;color:#fff;font-weight:700'>Total Outstanding</td>
            <td style='padding:10px 12px;color:#fff;font-weight:700;text-align:right'>NPR {total:N2}</td>
          </tr>
        </tbody>
      </table>

      <p style='color:#8b91a5;font-size:12px'>Please contact us if you have any questions about your balance.</p>
    </div>
  </div>
</body>
</html>";

            await SendAsync(to, subject, body);
        }

        // ── Internal send helper ──────────────────────────────────
        private async Task SendAsync(string? to, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogWarning("Email skipped: no recipient address.");
                return;
            }

            // If SMTP is not configured, log to console (dev mode)
            if (string.IsNullOrWhiteSpace(_opts.SmtpServer) || string.IsNullOrWhiteSpace(_opts.EmailFrom))
            {
                _logger.LogInformation("[EMAIL - SMTP not configured] To: {To} | Subject: {Subject}", to, subject);
                return;
            }

            try
            {
                using var client = new SmtpClient(_opts.SmtpServer, _opts.SmtpPort)
                {
                    EnableSsl      = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials    = new NetworkCredential(_opts.EmailFrom, _opts.EmailPassword),
                    Timeout        = _opts.TimeoutSeconds * 1000
                };

                using var message = new MailMessage
                {
                    From       = new MailAddress(_opts.EmailFrom, "VehicleParts Pro"),
                    Subject    = subject,
                    Body       = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(to);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
                // Don't rethrow — email failure should not break the main request
            }
        }
    }
}
