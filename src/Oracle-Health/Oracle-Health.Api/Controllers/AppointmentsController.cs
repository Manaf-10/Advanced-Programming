using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;

    public AppointmentsController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<LookupResponseViewModel>> Lookup(long patientReference, long cpr)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .Include(patient => patient.User)
            .FirstOrDefaultAsync(patient =>
                patient.PatientId == patientReference &&
                patient.Cpr == cpr);

        if (patient is null)
        {
            return NotFound(new { message = "Patient not found." });
        }

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(appointment =>
                appointment.PatientId == patient.Id &&
                appointment.Status != AppointmentStatus.Completed &&
                appointment.Status != AppointmentStatus.Cancelled &&
                appointment.Status != AppointmentStatus.Missed)
            .Include(appointment => appointment.Doctor)
                .ThenInclude(doctor => doctor.User)
            .Include(appointment => appointment.Doctor)
                .ThenInclude(doctor => doctor.Specializations)
            .OrderBy(appointment => appointment.Date)
            .Select(appointment => new LookupAppointmentViewModel
            {
                DoctorName = "Dr. " + appointment.Doctor.User.FirstName + " " + appointment.Doctor.User.LastName,
                Date = appointment.Date,
                Status = AppointmentStatus.ToDisplayName(appointment.Status),
                Specialization = appointment.Doctor.Specializations
                    .Select(specialization => specialization.Name)
                    .FirstOrDefault() ?? "General"
            })
            .ToListAsync();

        return new LookupResponseViewModel
        {
            PatientName = patient.User.FirstName + " " + patient.User.LastName,
            Appointments = appointments
        };
    }
}
