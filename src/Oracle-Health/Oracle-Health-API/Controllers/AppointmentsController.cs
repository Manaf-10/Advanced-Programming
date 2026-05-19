using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;

    public AppointmentsController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup(
        long patientReference,
        long cpr)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p =>
                p.PatientId == patientReference &&
                p.Cpr == cpr);

        if (patient == null)
        {
            return NotFound(new
            {
                message = "Patient not found."
            });
        }

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.PatientId == patient.Id &&
                a.Status != AppointmentStatus.Completed &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Missed)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
            .OrderBy(a => a.Date)
            .Select(a => new
            {
                doctorName =
                    "Dr. " +
                    a.Doctor.User.FirstName +
                    " " +
                    a.Doctor.User.LastName,

                date = a.Date,

                status = AppointmentStatus.ToDisplayName(a.Status)
            })
            .ToListAsync();

        return Ok(new LookupResponseViewModel
        {
            PatientName =
                patient.User.FirstName +
                " " +
                patient.User.LastName,

            Appointments = appointments.Select(a => new LookupAppointmentViewModel
            {
                DoctorName = a.doctorName,
                Date = a.date,
                Status = a.status,
                Specialization = "General"
            }).ToList()
        });
    }
}