using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Api.Dtos;
using Oracle_Health.Models;

namespace Oracle_Health.Api.Controllers;

[ApiController]
[Authorize(Roles = "Clinic Manager")]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;

    public UsersController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpGet("staff")]
    public async Task<ActionResult<IReadOnlyList<StaffUserDto>>> GetStaff()
    {
        var staff = await _context.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRole.Doctor || user.Role == UserRole.Receptionist)
            .OrderBy(user => user.Role)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new StaffUserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                UserRole.ToClaimValue(user.Role)))
            .ToListAsync();

        return staff;
    }
}
