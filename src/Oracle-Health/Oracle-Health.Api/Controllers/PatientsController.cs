using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Api.Dtos;
using Oracle_Health.Models;

namespace Oracle_Health.Api.Controllers;

[ApiController]
[Authorize(Roles = "Clinic Manager,Receptionist,Doctor")]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;

    public PatientsController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpGet("{id:long}/history")]
    public async Task<ActionResult<IReadOnlyList<VisitDto>>> GetHistory(long id)
    {
        var patientExists = await _context.Patients.AnyAsync(item => item.Id == id);
        if (!patientExists)
        {
            return NotFound();
        }

        if (!await CanViewPatientHistory(id))
        {
            return Forbid();
        }

        var visits = await _context.Visits
            .AsNoTracking()
            .Include(item => item.Appointment)
            .Include(item => item.Doctor)
                .ThenInclude(doctor => doctor.User)
            .Where(item => item.PatientId == id)
            .OrderByDescending(item => item.Appointment.Date)
            .Select(item => new VisitDto(
                item.Id,
                item.Appointment.Date,
                "Dr. " + item.Doctor.User.FirstName + " " + item.Doctor.User.LastName,
                item.Notes,
                item.Prescription))
            .ToListAsync();

        return visits;
    }

    private async Task<bool> CanViewPatientHistory(long patientId)
    {
        if (User.IsInRole("Clinic Manager") || User.IsInRole("Receptionist"))
        {
            return true;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return false;
        }

        if (User.IsInRole("Patient"))
        {
            return await _context.Patients.AnyAsync(item => item.Id == patientId && item.UserId == userId);
        }

        if (User.IsInRole("Doctor"))
        {
            return await _context.Appointments.AnyAsync(item =>
                item.PatientId == patientId && item.Doctor.UserId == userId);
        }

        return false;
    }
}
