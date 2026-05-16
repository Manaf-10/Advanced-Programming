using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;

namespace Oracle_Health.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ClinicManagementSystemContext _context;

    public NotificationsController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value);

        if (notification is not null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        return RedirectToPreviousPage();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var notifications = await _context.Notifications
            .Where(item => item.UserId == userId.Value)
            .ToListAsync();

        if (notifications.Count > 0)
        {
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();
        }

        return RedirectToPreviousPage();
    }

    private long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private IActionResult RedirectToPreviousPage()
    {
        var referer = Request.Headers.Referer.ToString();
        if (Url.IsLocalUrl(referer))
        {
            return Redirect(referer);
        }

        if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri) &&
            string.Equals(refererUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(refererUri.PathAndQuery);
        }

        return RedirectToAction("Index", "Home");
    }
}
