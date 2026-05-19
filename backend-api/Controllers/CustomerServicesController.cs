using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Models;
using VehiclePartsAPI.DTOs;

/// <summary>
/// Feature 13 — Customers can book service appointments, request unavailable parts, and submit reviews.
///
/// Endpoints:
///   GET    /api/appointments                       → All appointments (staff/admin)
///   GET    /api/appointments/customer/{id}         → Customer's own appointments
///   POST   /api/appointments                       → Book an appointment
///   PATCH  /api/appointments/{id}/status           → Update status (Confirmed/Completed/Cancelled)
///   DELETE /api/appointments/{id}                  → Cancel (customer or admin)
///
///   GET    /api/part-requests                      → All part requests
///   GET    /api/part-requests/customer/{id}        → Customer's requests
///   POST   /api/part-requests                      → Submit a part request
///   PATCH  /api/part-requests/{id}/status          → Update status (Fulfilled/Rejected)
///
///   GET    /api/reviews                            → All public reviews
///   POST   /api/reviews                            → Submit a review
///   DELETE /api/reviews/{id}                       → Admin removes a review
/// </summary>
[ApiController]
public class CustomerServicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomerServicesController(AppDbContext context) => _context = context;



    [HttpGet("api/appointments")]
    public async Task<IActionResult> GetAllAppointments([FromQuery] string? status)
    {
        var query = _context.Appointments
            .Include(a => a.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        var result = await query
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new AppointmentDto
            {
                Id              = a.Id,
                CustomerId      = a.CustomerId,
                CustomerName    = $"{a.Customer.FirstName} {a.Customer.LastName}",
                CustomerPhone   = a.Customer.Phone,
                ServiceType     = a.ServiceType,
                AppointmentDate = a.AppointmentDate,
                Notes           = a.Notes,
                Status          = a.Status,
                CreatedAt       = a.CreatedAt
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("api/appointments/customer/{customerId:int}")]
    public async Task<IActionResult> GetCustomerAppointments(int customerId)
    {
        var exists = await _context.Customers.AnyAsync(c => c.Id == customerId);
        if (!exists)
            return NotFound(new { message = $"Customer {customerId} not found." });

        var result = await _context.Appointments
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new AppointmentDto
            {
                Id              = a.Id,
                CustomerId      = a.CustomerId,
                CustomerName    = $"{a.Customer.FirstName} {a.Customer.LastName}",
                CustomerPhone   = a.Customer.Phone,
                ServiceType     = a.ServiceType,
                AppointmentDate = a.AppointmentDate,
                Notes           = a.Notes,
                Status          = a.Status,
                CreatedAt       = a.CreatedAt
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpPost("api/appointments")]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            return NotFound(new { message = $"Customer {dto.CustomerId} not found." });

        if (dto.AppointmentDate <= DateTime.UtcNow)
            return BadRequest(new { message = "Appointment date must be in the future." });

        var appointment = new Appointment
        {
            CustomerId      = dto.CustomerId,
            ServiceType     = dto.ServiceType,
            AppointmentDate = dto.AppointmentDate,
            Notes           = dto.Notes,
            Status          = "Pending"
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(null, new { id = appointment.Id }, new AppointmentDto
        {
            Id              = appointment.Id,
            CustomerId      = appointment.CustomerId,
            CustomerName    = $"{customer.FirstName} {customer.LastName}",
            CustomerPhone   = customer.Phone,
            ServiceType     = appointment.ServiceType,
            AppointmentDate = appointment.AppointmentDate,
            Notes           = appointment.Notes,
            Status          = appointment.Status,
            CreatedAt       = appointment.CreatedAt
        });
    }

    [HttpPatch("api/appointments/{id:int}/status")]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var allowed = new[] { "Confirmed", "Completed", "Cancelled" };
        if (!allowed.Contains(dto.Status))
            return BadRequest(new { message = $"Allowed statuses: {string.Join(", ", allowed)}" });

        var appt = await _context.Appointments.FindAsync(id);
        if (appt == null)
            return NotFound(new { message = $"Appointment {id} not found." });

        appt.Status = dto.Status;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("api/appointments/{id:int}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var appt = await _context.Appointments.FindAsync(id);
        if (appt == null)
            return NotFound(new { message = $"Appointment {id} not found." });

        _context.Appointments.Remove(appt);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ══════════════════════════════════════════════════════════
    // PART REQUESTS
    // ══════════════════════════════════════════════════════════

    [HttpGet("api/part-requests")]
    public async Task<IActionResult> GetAllPartRequests([FromQuery] string? status)
    {
        var query = _context.PartRequests.Include(r => r.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        var result = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PartRequestDto
            {
                Id           = r.Id,
                CustomerId   = r.CustomerId,
                CustomerName = $"{r.Customer.FirstName} {r.Customer.LastName}",
                PartName     = r.PartName,
                PartNumber   = r.PartNumber,
                Quantity     = r.Quantity,
                Notes        = r.Notes,
                Status       = r.Status,
                CreatedAt    = r.CreatedAt
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("api/part-requests/customer/{customerId:int}")]
    public async Task<IActionResult> GetCustomerPartRequests(int customerId)
    {
        var result = await _context.PartRequests
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PartRequestDto
            {
                Id           = r.Id,
                CustomerId   = r.CustomerId,
                CustomerName = $"{r.Customer.FirstName} {r.Customer.LastName}",
                PartName     = r.PartName,
                PartNumber   = r.PartNumber,
                Quantity     = r.Quantity,
                Notes        = r.Notes,
                Status       = r.Status,
                CreatedAt    = r.CreatedAt
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpPost("api/part-requests")]
    public async Task<IActionResult> CreatePartRequest([FromBody] CreatePartRequestDto dto)
    {
        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            return NotFound(new { message = $"Customer {dto.CustomerId} not found." });

        var req = new PartRequest
        {
            CustomerId = dto.CustomerId,
            PartName   = dto.PartName,
            PartNumber = dto.PartNumber,
            Quantity   = dto.Quantity,
            Notes      = dto.Notes,
            Status     = "Pending"
        };

        _context.PartRequests.Add(req);
        await _context.SaveChangesAsync();
        return CreatedAtAction(null, new { id = req.Id }, new PartRequestDto
        {
            Id           = req.Id,
            CustomerId   = req.CustomerId,
            CustomerName = $"{customer.FirstName} {customer.LastName}",
            PartName     = req.PartName,
            PartNumber   = req.PartNumber,
            Quantity     = req.Quantity,
            Notes        = req.Notes,
            Status       = req.Status,
            CreatedAt    = req.CreatedAt
        });
    }

    [HttpPatch("api/part-requests/{id:int}/status")]
    public async Task<IActionResult> UpdatePartRequestStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var allowed = new[] { "Fulfilled", "Rejected" };
        if (!allowed.Contains(dto.Status))
            return BadRequest(new { message = $"Allowed statuses: {string.Join(", ", allowed)}" });

        var req = await _context.PartRequests.FindAsync(id);
        if (req == null)
            return NotFound(new { message = $"Part request {id} not found." });

        req.Status = dto.Status;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ══════════════════════════════════════════════════════════
    // REVIEWS
    // ══════════════════════════════════════════════════════════

    [HttpGet("api/reviews")]
    public async Task<IActionResult> GetAllReviews()
    {
        var reviews = await _context.ServiceReviews
            .Include(r => r.Customer)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                Id           = r.Id,
                CustomerId   = r.CustomerId,
                CustomerName = $"{r.Customer.FirstName} {r.Customer.LastName}",
                Rating       = r.Rating,
                Comment      = r.Comment,
                CreatedAt    = r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpPost("api/reviews")]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest(new { message = "Rating must be between 1 and 5." });

        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            return NotFound(new { message = $"Customer {dto.CustomerId} not found." });

        var review = new ServiceReview
        {
            CustomerId = dto.CustomerId,
            Rating     = dto.Rating,
            Comment    = dto.Comment
        };

        _context.ServiceReviews.Add(review);
        await _context.SaveChangesAsync();

        return CreatedAtAction(null, new { id = review.Id }, new ReviewDto
        {
            Id           = review.Id,
            CustomerId   = review.CustomerId,
            CustomerName = $"{customer.FirstName} {customer.LastName}",
            Rating       = review.Rating,
            Comment      = review.Comment,
            CreatedAt    = review.CreatedAt
        });
    }

    [HttpDelete("api/reviews/{id:int}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.ServiceReviews.FindAsync(id);
        if (review == null)
            return NotFound(new { message = $"Review {id} not found." });

        _context.ServiceReviews.Remove(review);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PATCH /api/customers/{id}/password
    [HttpPatch("api/customers/{id:int}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return NotFound(new { message = $"Customer {id} not found." });

        customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Password updated successfully." });
    }
}
