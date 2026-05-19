namespace VehiclePartsAPI.Models;

// ── Appointment (Feature 13) ──────────────────────────────────
public class Appointment
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>e.g. "Oil Change", "Brake Inspection", "General Service"</summary>
    public string ServiceType { get; set; } = string.Empty;

    public DateTime AppointmentDate { get; set; }

    public string Notes { get; set; } = string.Empty;

    /// <summary>Pending | Confirmed | Completed | Cancelled</summary>
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Part Request (Feature 13) ─────────────────────────────────
public class PartRequest
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string PartName   { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public int    Quantity   { get; set; } = 1;
    public string Notes      { get; set; } = string.Empty;

    /// <summary>Pending | Fulfilled | Rejected</summary>
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ── Service Review (Feature 13) ───────────────────────────────
public class ServiceReview
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>1–5 stars</summary>
    public int    Rating  { get; set; }
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
