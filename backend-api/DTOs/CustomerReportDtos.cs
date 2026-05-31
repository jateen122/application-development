namespace VehiclePartsAPI.DTOs;

public class CustomerSpendDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal TotalSpend { get; set; }
    public int InvoiceCount { get; set; }
    public int LoyaltyInvoices { get; set; }
    public DateTime? LastPurchase { get; set; }
}

/// <summary>Used by pending-credits and overdue-credits endpoints.</summary>
public class CustomerCreditDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int CreditInvoiceCount { get; set; }
    public decimal TotalCreditAmount { get; set; }
    public DateTime? OldestCreditDate { get; set; }
}

/// <summary>Used by the universal search endpoint.</summary>
public class CustomerSearchResultDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> MatchedVehicles { get; set; } = new();
}
