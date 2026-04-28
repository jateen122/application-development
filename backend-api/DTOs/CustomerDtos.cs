namespace VehiclePartsAPI.DTOs
{
    // ─────────────────────────────────────────────
    // BASIC CUSTOMER DTO
    // ─────────────────────────────────────────────
    public class CustomerDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ─────────────────────────────────────────────
    // CUSTOMER DETAIL (WITH VEHICLES + PURCHASES)
    // ─────────────────────────────────────────────
    public class CustomerDetailDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<VehicleDto> Vehicles { get; set; } = new();
        public List<SaleInvoiceSummaryDto> RecentPurchases { get; set; } = new();
    }

    // ─────────────────────────────────────────────
    // REGISTER (STAFF)
    // ─────────────────────────────────────────────
    public class RegisterCustomerDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public CreateVehicleDto? Vehicle { get; set; }
    }

    // ─────────────────────────────────────────────
    // SELF REGISTER (CUSTOMER)
    // ─────────────────────────────────────────────
    public class SelfRegisterCustomerDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────
    // UPDATE CUSTOMER
    // ─────────────────────────────────────────────
    public class UpdateCustomerDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────
    // VEHICLE CREATE DTO
    // ─────────────────────────────────────────────
    public class CreateVehicleDto
    {
        public string VehicleNumber { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string FuelType { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────
    // VEHICLE RESPONSE DTO
    // ─────────────────────────────────────────────
    public class VehicleDto
    {
        public int Id { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string FuelType { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    // NOTE: SaleInvoiceSummaryDto is defined in SaleDtos.cs — do NOT duplicate it here.
}
