using System.Text.Json.Serialization;

namespace VehiclePartsAPI.DTOs;

// ── Request DTOs ──────────────────────────────────────────────

public class KhaltiInitiateDto
{
    public int    CustomerId  { get; set; }
    public string? WebsiteUrl { get; set; }
    public List<KhaltiItemDto> Items { get; set; } = new();
}

public class KhaltiItemDto
{
    public int PartId   { get; set; }
    public int Quantity { get; set; }
}

public class KhaltiLookupDto
{
    public string Pidx { get; set; } = string.Empty;
}

// ── Khalti API Response DTOs ──────────────────────────────────

public class KhaltiInitiateResponse
{
    [JsonPropertyName("pidx")]
    public string Pidx { get; set; } = string.Empty;

    [JsonPropertyName("payment_url")]
    public string PaymentUrl { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public string ExpiresAt { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public class KhaltiLookupResponse
{
    [JsonPropertyName("pidx")]
    public string Pidx { get; set; } = string.Empty;

    [JsonPropertyName("total_amount")]
    public int TotalAmount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("fee")]
    public int Fee { get; set; }

    [JsonPropertyName("refunded")]
    public bool Refunded { get; set; }
}
