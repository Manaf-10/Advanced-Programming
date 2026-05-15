using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;
using Oracle_Health.Services;

namespace Oracle_Health.Controllers;

public class AccountController : Controller
{
    private readonly ClinicManagementSystemContext _context;

    public AccountController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLower();
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Email.ToLower() == normalizedEmail);

        if (user is null || !PasswordService.Verify(model.Password, user.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        await SignInUser(user, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLower();
        var emailExists = await _context.Users.AnyAsync(item => item.Email.ToLower() == normalizedEmail);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var cprExists = await _context.Patients.AnyAsync(item => item.Cpr == model.Cpr);
        if (cprExists)
        {
            ModelState.AddModelError(nameof(model.Cpr), "A patient account with this CPR already exists.");
            return View(model);
        }

        var user = new User
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = normalizedEmail,
            Password = PasswordService.Hash(model.Password),
            Role = UserRole.Patient
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.Patients.Add(new Patient
        {
            UserId = user.Id,
            PatientId = await GeneratePatientReference(),
            Cpr = model.Cpr!.Value
        });
        await _context.SaveChangesAsync();

        await SignInUser(user, rememberMe: false);
        return RedirectToAction("Profile");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return RedirectToAction("Login");
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(item => item.Patients)
            .FirstOrDefaultAsync(item => item.Id == userId);

        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        var patient = user.Patients.FirstOrDefault();
        return View(new ProfileViewModel
        {
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email,
            Role = UserRole.ToDisplayName(user.Role),
            PatientReference = patient?.PatientId,
            Cpr = patient?.Cpr
        });
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInUser(User user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, UserRole.ToClaimValue(user.Role))
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
            });
    }

    private async Task<long> GeneratePatientReference()
    {
        var nextReference = 10_000L;
        var latestReference = await _context.Patients
            .OrderByDescending(item => item.PatientId)
            .Select(item => (long?)item.PatientId)
            .FirstOrDefaultAsync();

        if (latestReference.HasValue && latestReference.Value >= nextReference)
        {
            nextReference = latestReference.Value + 1;
        }

        return nextReference;
    }
}
