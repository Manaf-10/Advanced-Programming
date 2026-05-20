using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Oracle_Health.Models;
using Oracle_Health.Services;

namespace Oracle_Health.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(ClinicManagementSystemContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("token")]
    public async Task<ActionResult<TokenResponse>> CreateToken(TokenRequest request)
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
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, role)
        };

        var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is missing.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new TokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            role,
            $"{user.FirstName} {user.LastName}");
    }
}

public record TokenRequest(string Email, string Password);

public record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, string Role, string FullName);
