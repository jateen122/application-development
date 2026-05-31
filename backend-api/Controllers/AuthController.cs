using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Data;
using VehiclePartsAPI.DTOs;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context) => _context = context;

    // POST /api/auth/login
    // Accepts { email, password, role }.
    // Checks Staff table for Admin/Staff roles, Customers table for Customer role.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Email and password are required." });

        // Staff / Admin login 
        if (dto.Role == "Admin" || dto.Role == "Staff")
        {
            var staff = await _context.Staff
                .FirstOrDefaultAsync(s => s.Email == dto.Email);

            if (staff == null)
                return Unauthorized(new { message = "Invalid email or password." });

            if (!staff.IsActive)
                return Unauthorized(new { message = "This account has been deactivated. Contact your administrator." });

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, staff.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            // Ensure the role matches what the user claims
            if (staff.Role != dto.Role)
                return Unauthorized(new { message = $"This account does not have the '{dto.Role}' role." });

            return Ok(new LoginResponseDto
            {
                Id = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Email = staff.Email,
                Role = staff.Role
            });
        }

        //Customer login 
        if (dto.Role == "Customer")
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == dto.Email);

            if (customer == null)
                return Unauthorized(new { message = "Invalid email or password." });

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            return Ok(new LoginResponseDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Role = "Customer"
            });
        }

        return BadRequest(new { message = "Invalid role. Must be Admin, Staff, or Customer." });
    }
}
