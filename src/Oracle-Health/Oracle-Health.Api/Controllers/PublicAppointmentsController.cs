using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;

namespace Oracle_Health.Api.Controllers;

[ApiController]
[Route("api/public/appointments")]
public class PublicAppointmentsController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;

    public PublicAppointmentsController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpPost("lookup")]
    public async Task<ActionResult<PublicAppointmentLookupResponse>> Lookup(PublicAppointmentLookupRequest request)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Cpr == request.Cpr && item.PatientId == request.PatientReference);

        if (patient is null)
        {
            return NotFound();
        }

        var now = DateTime.Now;
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(item => item.Doctor)
                .ThenInclude(doctor => doctor.User)
            .Where(item => item.PatientId == patient.Id && item.Date >= now)
            .OrderBy(item => item.Date)
            .Take(5)
            .Select(item => new PublicAppointmentItem(
                item.Id,
                item.Date,
                item.Doctor.User.FirstName + " " + item.Doctor.User.LastName,
                AppointmentStatus.ToDisplayName(item.Status)))
            .ToListAsync();

        var recentVisits = await _context.Visits
            .AsNoTracking()
            .Include(item => item.Appointment)
            .Include(item => item.Doctor)
                .ThenInclude(doctor => doctor.User)
            .Where(item => item.PatientId == patient.Id)
            .OrderByDescending(item => item.Appointment.Date)
            .Take(3)
            .Select(item => new PublicVisitSummary(
                item.Appointment.Date,
                item.Doctor.User.FirstName + " " + item.Doctor.User.LastName,
                item.Notes))
            .ToListAsync();

        return new PublicAppointmentLookupResponse(
            $"{patient.User.FirstName} {patient.User.LastName}",
            appointments,
            recentVisits);
    }
}

public record PublicAppointmentLookupRequest(long Cpr, long PatientReference);

public record PublicAppointmentLookupResponse(
    string PatientName,
    IReadOnlyList<PublicAppointmentItem> UpcomingAppointments,
    IReadOnlyList<PublicVisitSummary> RecentVisits);

public record PublicAppointmentItem(long Id, DateTime Date, string DoctorName, string Status);

public record PublicVisitSummary(DateTime AppointmentDate, string DoctorName, string Summary);
