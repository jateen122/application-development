namespace VehiclePartsAPI.Models;

public class SaleInvoice
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;   // SI-2026-0001
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public bool LoyaltyDiscountApplied { get; set; }

    // Payment Details
    public string PaymentMethod { get; set; } = string.Empty;   // Cash | Card | Credit | Khalti
    public string Status { get; set; } = "Paid";                // Paid | Credit | Cancelled | Pending

    // Khalti Payment Fields
    public string? KhaltiPidx { get; set; }
    public string? KhaltiTransactionId { get; set; }
    public string? KhaltiPurchaseOrderId { get; set; }
    public string? KhaltiReferenceId { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public List<SaleInvoiceItem> Items { get; set; } = new();
}