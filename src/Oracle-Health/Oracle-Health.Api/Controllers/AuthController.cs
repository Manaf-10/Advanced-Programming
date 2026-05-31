using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Api.Dtos;
using Oracle_Health.Api.Services;
using Oracle_Health.Models;
using Oracle_Health.Services;

namespace Oracle_Health.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;
    private readonly ITokenService _tokenService;

    public AuthController(ClinicManagementSystemContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }
    
    [HttpPost("token")]
    public async Task<ActionResult<LoginResponse>> CreateToken(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLower();
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Email.ToLower() == normalizedEmail);

        if (user is null || !PasswordService.Verify(request.Password, user.Password))
        {
            return Unauthorized();
        }

        var role = UserRole.ToClaimValue(user.Role);
        var expiresAt = DateTime.UtcNow.AddHours(8);
        var token = _tokenService.CreateToken(user, role);

        return Ok(new LoginResponse(
            token,
            expiresAt,
            role,
            $"{user.FirstName} {user.LastName}"));
    }
}
