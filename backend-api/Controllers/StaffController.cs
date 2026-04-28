using VehiclePartsAPI.Models;
using VehiclePartsAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Feature 2 — Admin can manage staff registration and roles.
///
/// Endpoints:
///   GET    /api/staff                        → List all staff
///   GET    /api/staff/{id}                   → Get staff member by ID
///   POST   /api/staff                        → Register a new staff member (Admin only)
///   PUT    /api/staff/{id}                   → Update staff contact details
///   PATCH  /api/staff/{id}/role              → Change staff role (Staff ↔ Admin)
///   PATCH  /api/staff/{id}/status            → Activate / deactivate a staff account
///   DELETE /api/staff/{id}                   → Permanently remove a staff member
///   GET    /api/staff/active                 → List only active staff
///   GET    /api/staff/count                  → Total staff count
/// </summary>
[ApiController]
[Route("api/staff")]
public class StaffController : ControllerBase
{
    private readonly AppDbContext _context;

    // Roles allowed in the system
    private static readonly string[] AllowedRoles = { "Staff", "Admin" };

    public StaffController(AppDbContext context) => _context = context;

    // ══════════════════════════════════════════════════════════
    // READ
    // ══════════════════════════════════════════════════════════

    // GET /api/staff
    // Returns all staff members ordered by name.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var staff = await _context.Staff
            .OrderBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .Select(s => new StaffDto
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.Email,
                Phone = s.Phone,
                Role = s.Role,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return Ok(staff);
    }

    // GET /api/staff/{id}
    // Returns a single staff member by ID.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var staff = await _context.Staff.FindAsync(id);
        if (staff == null)
            return NotFound(new { message = $"Staff member with ID {id} not found." });

        return Ok(MapToDto(staff));
    }

    // GET /api/staff/active
    // Returns only active (non-deactivated) staff members.
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var staff = await _context.Staff
            .Where(s => s.IsActive)
            .OrderBy(s => s.FirstName)
            .Select(s => new StaffDto
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.Email,
                Phone = s.Phone,
                Role = s.Role,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return Ok(staff);
    }

    // GET /api/staff/count
    // Returns total and active staff counts.
    [HttpGet("count")]
    public async Task<IActionResult> Count()
    {
        var total = await _context.Staff.CountAsync();
        var active = await _context.Staff.CountAsync(s => s.IsActive);
        return Ok(new { totalStaff = total, activeStaff = active, inactiveStaff = total - active });
    }

    // ══════════════════════════════════════════════════════════
    // CREATE
    // ══════════════════════════════════════════════════════════

    // POST /api/staff
    // Admin registers a new staff member.
    // Password is hashed with BCrypt before storage.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffDto dto)
    {
        // Validate role value
        if (!AllowedRoles.Contains(dto.Role))
            return BadRequest(new { message = $"Invalid role '{dto.Role}'. Allowed: {string.Join(", ", AllowedRoles)}" });

        // Email must be unique across all staff
        var emailExists = await _context.Staff.AnyAsync(s => s.Email == dto.Email);
        if (emailExists)
            return Conflict(new { message = "A staff member with this email already exists." });

        var staff = new Staff
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            Role = dto.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsActive = true
        };

        _context.Staff.Add(staff);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = staff.Id }, MapToDto(staff));
    }

    // ══════════════════════════════════════════════════════════
    // UPDATE
    // ══════════════════════════════════════════════════════════

    // PUT /api/staff/{id}
    // Updates a staff member's contact details (name, email, phone).
    // Role and status are managed through their dedicated PATCH endpoints.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStaffDto dto)
    {
        var staff = await _context.Staff.FindAsync(id);
        if (staff == null)
            return NotFound(new { message = $"Staff member with ID {id} not found." });

        // Prevent email collision with another staff member
        var emailConflict = await _context.Staff.AnyAsync(s => s.Email == dto.Email && s.Id != id);
        if (emailConflict)
            return Conflict(new { message = "Another staff member is already using this email." });

        staff.FirstName = dto.FirstName;
        staff.LastName = dto.LastName;
        staff.Email = dto.Email;
        staff.Phone = dto.Phone;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PATCH /api/staff/{id}/role
    // Changes a staff member's role (e.g. promote Staff → Admin or demote Admin → Staff).
    [HttpPatch("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateStaffRoleDto dto)
    {
        if (!AllowedRoles.Contains(dto.Role))
            return BadRequest(new { message = $"Invalid role '{dto.Role}'. Allowed: {string.Join(", ", AllowedRoles)}" });

        var staff = await _context.Staff.FindAsync(id);
        if (staff == null)
            return NotFound(new { message = $"Staff member with ID {id} not found." });

        if (staff.Role == dto.Role)
            return BadRequest(new { message = $"Staff member already has role '{dto.Role}'." });

        // Safety check: cannot demote the last active Admin
        if (staff.Role == "Admin" && dto.Role == "Staff")
        {
            var adminCount = await _context.Staff.CountAsync(s => s.Role == "Admin" && s.IsActive);
            if (adminCount <= 1)
                return BadRequest(new { message = "Cannot demote the last active Admin. Promote another staff member first." });
        }

        staff.Role = dto.Role;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Role updated to '{dto.Role}' successfully.", staff = MapToDto(staff) });
    }

    // PATCH /api/staff/{id}/status
    // Activates or deactivates a staff account without deleting it.
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStaffStatusDto dto)
    {
        var staff = await _context.Staff.FindAsync(id);
        if (staff == null)
            return NotFound(new { message = $"Staff member with ID {id} not found." });

        // Prevent deactivating the last active Admin
        if (!dto.IsActive && staff.Role == "Admin")
        {
            var activeAdminCount = await _context.Staff.CountAsync(s => s.Role == "Admin" && s.IsActive);
            if (activeAdminCount <= 1)
                return BadRequest(new { message = "Cannot deactivate the last active Admin account." });
        }

        staff.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();

        var statusLabel = dto.IsActive ? "activated" : "deactivated";
        return Ok(new { message = $"Staff account {statusLabel} successfully.", staff = MapToDto(staff) });
    }

    // ══════════════════════════════════════════════════════════
    // DELETE
    // ══════════════════════════════════════════════════════════

    // DELETE /api/staff/{id}
    // Permanently removes a staff member.
    // Consider using PATCH /status to deactivate instead of hard-delete.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var staff = await _context.Staff.FindAsync(id);
        if (staff == null)
            return NotFound(new { message = $"Staff member with ID {id} not found." });

        // Cannot delete the last active Admin
        if (staff.Role == "Admin")
        {
            var activeAdminCount = await _context.Staff.CountAsync(s => s.Role == "Admin" && s.IsActive);
            if (activeAdminCount <= 1)
                return BadRequest(new { message = "Cannot delete the last Admin. Assign another Admin first." });
        }

        _context.Staff.Remove(staff);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ── Helper ─────────────────────────────────────────────────
    private static StaffDto MapToDto(Staff s) => new()
    {
        Id = s.Id,
        FirstName = s.FirstName,
        LastName = s.LastName,
        Email = s.Email,
        Phone = s.Phone,
        Role = s.Role,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt
    };
}
