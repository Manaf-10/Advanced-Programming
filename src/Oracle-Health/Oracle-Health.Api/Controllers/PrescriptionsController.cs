using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Api.Dtos;
using Oracle_Health.Models;

namespace Oracle_Health.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/prescriptions")]
public class PrescriptionsController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;

    public PrescriptionsController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PrescriptionDto>> GetDetails(long id)
    {
        var visit = await _context.Visits
            .AsNoTracking()
            .Include(item => item.Appointment)
            .Include(item => item.Patient)
                .ThenInclude(patient => patient.User)
            .Include(item => item.Doctor)
                .ThenInclude(doctor => doctor.User)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (visit is null)
        {
            return NotFound();
        }

        if (!await CanViewPrescription(visit))
        {
            return Forbid();
        }

        return new PrescriptionDto(
            visit.Id,
            visit.PatientId,
            visit.Patient.User.FirstName + " " + visit.Patient.User.LastName,
            "Dr. " + visit.Doctor.User.FirstName + " " + visit.Doctor.User.LastName,
            visit.Appointment.Date,
            visit.Prescription,
            visit.Notes);
    }

    private async Task<bool> CanViewPrescription(Visit visit)
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
            return await _context.Patients.AnyAsync(item => item.Id == visit.PatientId && item.UserId == userId);
        }

        if (User.IsInRole("Doctor"))
        {
            return await _context.Doctors.AnyAsync(item => item.Id == visit.DoctorId && item.UserId == userId);
        }

        return false;
    }
}
