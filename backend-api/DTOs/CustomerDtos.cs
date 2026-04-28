namespace VehiclePartsAPI.DTOs
{
    // ─────────────────────────────────────────────
    // BASIC CUSTOMER DTO
    // ─────────────────────────────────────────────
    public class CustomerDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─────────────────────────────────────────────
    // CUSTOMER DETAIL (WITH VEHICLES + PURCHASES)
    // ─────────────────────────────────────────────
    public class CustomerDetailDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<VehicleDto> Vehicles { get; set; } = new();
        public List<SaleInvoiceSummaryDto> RecentPurchases { get; set; } = new();
    }

    // ─────────────────────────────────────────────
    // REGISTER (STAFF)
    // ─────────────────────────────────────────────
    public class RegisterCustomerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Password { get; set; }

        // 🔥 IMPORTANT FIX (used in controller)
        public CreateVehicleDto? Vehicle { get; set; }
    }

    // ─────────────────────────────────────────────
    // SELF REGISTER (CUSTOMER)
    // ─────────────────────────────────────────────
    public class SelfRegisterCustomerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Password { get; set; }
    }

    // ─────────────────────────────────────────────
    // UPDATE CUSTOMER
    // ─────────────────────────────────────────────
    public class UpdateCustomerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }

    // ─────────────────────────────────────────────
    // VEHICLE CREATE DTO
    // ─────────────────────────────────────────────
    public class CreateVehicleDto
    {
        public string VehicleNumber { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string Color { get; set; }
        public string FuelType { get; set; }
    }

    // ─────────────────────────────────────────────
    // VEHICLE RESPONSE DTO
    // ─────────────────────────────────────────────
    public class VehicleDto
    {
        public int Id { get; set; }
        public string VehicleNumber { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string Color { get; set; }
        public string FuelType { get; set; }
        public int CustomerId { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    // ─────────────────────────────────────────────
    // SALE INVOICE SUMMARY (FOR CUSTOMER DETAIL)
    // ─────────────────────────────────────────────
    public class SaleInvoiceSummaryDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public bool LoyaltyDiscountApplied { get; set; }
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
    }
}