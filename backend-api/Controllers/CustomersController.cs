using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.DTOs;
using VehiclePartsAPI.Models;
using VehiclePartsAPI.Data;

namespace VehiclePartsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context) => _context = context;

        // GET /api/customers
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.FirstName)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Address = c.Address,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(customers);
        }

        // GET /api/customers/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Vehicles)
                .Include(c => c.SaleInvoices)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            return Ok(new CustomerDetailDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                CreatedAt = customer.CreatedAt,
                Vehicles = customer.Vehicles.Select(v => new VehicleDto
                {
                    Id = v.Id,
                    VehicleNumber = v.VehicleNumber,
                    Make = v.Make,
                    Model = v.Model,
                    Year = v.Year,
                    Color = v.Color,
                    FuelType = v.FuelType,
                    CustomerId = v.CustomerId,
                    RegisteredAt = v.RegisteredAt
                }).ToList(),
                RecentPurchases = customer.SaleInvoices
                    .OrderByDescending(si => si.InvoiceDate)
                    .Take(5)
                    .Select(si => new SaleInvoiceSummaryDto
                    {
                        Id = si.Id,
                        InvoiceNumber = si.InvoiceNumber,
                        InvoiceDate = si.InvoiceDate,
                        TotalAmount = si.TotalAmount,
                        LoyaltyDiscountApplied = si.LoyaltyDiscountApplied,
                        Status = si.Status,
                        PaymentMethod = si.PaymentMethod
                    }).ToList()
            });
        }

        // POST /api/customers/register  — Staff registers a customer
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCustomerDto dto)
        {
            if (await _context.Customers.AnyAsync(c => c.Email == dto.Email))
                return Conflict(new { message = "A customer with this email already exists." });

            var customer = new Customer
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Customer"
            };

            if (dto.Vehicle != null)
            {
                customer.Vehicles.Add(new Vehicle
                {
                    VehicleNumber = dto.Vehicle.VehicleNumber,
                    Make = dto.Vehicle.Make,
                    Model = dto.Vehicle.Model,
                    Year = dto.Vehicle.Year,
                    Color = dto.Vehicle.Color,
                    FuelType = dto.Vehicle.FuelType
                });
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, MapToDto(customer));
        }

        // POST /api/customers/self-register  — Customer registers themselves
        [HttpPost("self-register")]
        public async Task<IActionResult> SelfRegister([FromBody] SelfRegisterCustomerDto dto)
        {
            if (await _context.Customers.AnyAsync(c => c.Email == dto.Email))
                return Conflict(new { message = "An account with this email already exists." });

            var customer = new Customer
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Customer"
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, MapToDto(customer));
        }

        // PUT /api/customers/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            var emailConflict = await _context.Customers.AnyAsync(c => c.Email == dto.Email && c.Id != id);
            if (emailConflict)
                return Conflict(new { message = "Another customer is using this email." });

            customer.FirstName = dto.FirstName;
            customer.LastName = dto.LastName;
            customer.Email = dto.Email;
            customer.Phone = dto.Phone;
            customer.Address = dto.Address;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/customers/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET /api/customers/search?q=...
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Search query 'q' is required." });

            var term = q.Trim().ToLower();

            var results = await _context.Customers
                .Where(c =>
                    c.FirstName.ToLower().Contains(term) ||
                    c.LastName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term) ||
                    c.Phone.Contains(term))
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Address = c.Address,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(results);
        }

        // ══════════════════════════════════════════════════════════
        // VEHICLE SUB-RESOURCE
        // ══════════════════════════════════════════════════════════

        // GET /api/customers/{id}/vehicles
        [HttpGet("{id:int}/vehicles")]
        public async Task<IActionResult> GetVehicles(int id)
        {
            var exists = await _context.Customers.AnyAsync(c => c.Id == id);
            if (!exists)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            var vehicles = await _context.Vehicles
                .Where(v => v.CustomerId == id)
                .Select(v => new VehicleDto
                {
                    Id = v.Id,
                    VehicleNumber = v.VehicleNumber,
                    Make = v.Make,
                    Model = v.Model,
                    Year = v.Year,
                    Color = v.Color,
                    FuelType = v.FuelType,
                    CustomerId = v.CustomerId,
                    RegisteredAt = v.RegisteredAt
                })
                .ToListAsync();

            return Ok(vehicles);
        }

        // POST /api/customers/{id}/vehicles
        [HttpPost("{id:int}/vehicles")]
        public async Task<IActionResult> AddVehicle(int id, [FromBody] CreateVehicleDto dto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            var vnExists = await _context.Vehicles.AnyAsync(v => v.VehicleNumber == dto.VehicleNumber);
            if (vnExists)
                return Conflict(new { message = $"Vehicle number '{dto.VehicleNumber}' is already registered." });

            var vehicle = new Vehicle
            {
                VehicleNumber = dto.VehicleNumber,
                Make = dto.Make,
                Model = dto.Model,
                Year = dto.Year,
                Color = dto.Color,
                FuelType = dto.FuelType,
                CustomerId = id
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVehicles), new { id }, new VehicleDto
            {
                Id = vehicle.Id,
                VehicleNumber = vehicle.VehicleNumber,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                Color = vehicle.Color,
                FuelType = vehicle.FuelType,
                CustomerId = vehicle.CustomerId,
                RegisteredAt = vehicle.RegisteredAt
            });
        }

        // PUT /api/customers/{id}/vehicles/{vehicleId}
        [HttpPut("{id:int}/vehicles/{vehicleId:int}")]
        public async Task<IActionResult> UpdateVehicle(int id, int vehicleId, [FromBody] CreateVehicleDto dto)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.CustomerId == id);
            if (vehicle == null)
                return NotFound(new { message = "Vehicle not found for this customer." });

            var vnConflict = await _context.Vehicles.AnyAsync(v => v.VehicleNumber == dto.VehicleNumber && v.Id != vehicleId);
            if (vnConflict)
                return Conflict(new { message = $"Vehicle number '{dto.VehicleNumber}' is already registered." });

            vehicle.VehicleNumber = dto.VehicleNumber;
            vehicle.Make = dto.Make;
            vehicle.Model = dto.Model;
            vehicle.Year = dto.Year;
            vehicle.Color = dto.Color;
            vehicle.FuelType = dto.FuelType;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/customers/{id}/vehicles/{vehicleId}
        [HttpDelete("{id:int}/vehicles/{vehicleId:int}")]
        public async Task<IActionResult> DeleteVehicle(int id, int vehicleId)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.CustomerId == id);
            if (vehicle == null)
                return NotFound(new { message = "Vehicle not found for this customer." });

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── Helpers ───────────────────────────────────────────────
        private static CustomerDto MapToDto(Customer c) => new()
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            Phone = c.Phone,
            Address = c.Address,
            CreatedAt = c.CreatedAt
        };
    }
}
