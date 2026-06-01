using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Api.Dtos;
using Oracle_Health.Models;
using Oracle_Health.Services;

namespace Oracle_Health.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;
    private readonly IValidationService _validationService;

    public DoctorsController(ClinicManagementSystemContext context, IValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
    }

    [Authorize(Roles = "Clinic Manager")]
    [HttpPost("{id:long}/availability")]
    public async Task<IActionResult> UpdateAvailability(long id, ScheduleRequest request)
    {
        if (request.EndTime <= request.StartTime)
        {
            return BadRequest(new { message = "Availability end time must be after start time." });
        }

        var doctorExists = await _context.Doctors.AnyAsync(item => item.Id == id);
        if (!doctorExists)
        {
            return NotFound();
        }

        if (request.IsOnLeave)
        {
            var impactedAppointments = await _validationService.GetImpactedAppointments(id, request.StartTime, request.EndTime);
            if (impactedAppointments.Count > 0)
            {
                return BadRequest(new
                {
                    error = "Conflict",
                    message = $"There are {impactedAppointments.Count} appointments booked during this leave period. Reschedule them first.",
                    appointments = impactedAppointments.Select(item => new
                    {
                        item.Id,
                        item.Date,
                        PatientName = item.Patient.User.FirstName + " " + item.Patient.User.LastName
                    })
                });
            }
        }

        _context.Schedules.Add(new Schedule
        {
            DoctorId = id,
            DayOfWeek = request.StartTime.DayOfWeek.ToString(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsOnLeave = request.IsOnLeave
        });

        await _context.SaveChangesAsync();
        return NoContent();
    }
}

