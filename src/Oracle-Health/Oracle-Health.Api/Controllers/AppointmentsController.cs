using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Api.Dtos;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;
using Oracle_Health.Services;

namespace Oracle_Health.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;
    private readonly IValidationService _validationService;

    public AppointmentsController(ClinicManagementSystemContext context, IValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
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

    [Authorize(Roles = "Clinic Manager")]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, AppointmentUpdateRequest request)
    {
        var appointment = await _context.Appointments.FirstOrDefaultAsync(item => item.Id == id);
        if (appointment is null)
        {
            return NotFound();
        }

        var patientExists = await _context.Patients.AnyAsync(item => item.Id == request.PatientId);
        var doctorExists = await _context.Doctors.AnyAsync(item => item.Id == request.DoctorId);

        if (!patientExists || !doctorExists)
        {
            return BadRequest(new { message = "Select a valid patient and doctor." });
        }

        if (request.Status != AppointmentStatus.Cancelled && request.Status != AppointmentStatus.Missed)
        {
            var validation = await _validationService.CheckAppointmentConflict(
                request.DoctorId,
                request.Date,
                request.DurationMinutes,
                id);

            if (!validation.IsValid)
            {
                return BadRequest(new { message = validation.Message });
            }
        }

        appointment.PatientId = request.PatientId;
        appointment.DoctorId = request.DoctorId;
        appointment.Date = request.Date;
        appointment.DurationMinutes = request.DurationMinutes;
        appointment.Status = request.Status;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
