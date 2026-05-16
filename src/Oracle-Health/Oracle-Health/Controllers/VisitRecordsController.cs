using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health.Controllers;

[Authorize(Roles = "Doctor")]
[Route("visit-records")]
public class VisitRecordsController : Controller
{
    private readonly ClinicManagementSystemContext _context;

    public VisitRecordsController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var doctor = await GetCurrentDoctor();
        if (doctor is null)
        {
            return Forbid();
        }

        var patientIds = await GetCurrentDoctorPatientIds(doctor.Id);

        var visits = await _context.Visits
            .AsNoTracking()
            .Include(visit => visit.Patient)
                .ThenInclude(patient => patient.User)
            .Include(visit => visit.Doctor)
                .ThenInclude(doctor => doctor.User)
            .Include(visit => visit.Appointment)
            .Where(visit => patientIds.Contains(visit.PatientId))
            .OrderByDescending(visit => visit.Appointment.Date)
            .ThenByDescending(visit => visit.CreatedAt)
            .Select(visit => new VisitRecordListItemViewModel
            {
                Id = visit.Id,
                PatientName = visit.Patient.User.FirstName + " " + visit.Patient.User.LastName,
                DoctorName = visit.Doctor.User.FirstName + " " + visit.Doctor.User.LastName,
                AppointmentDate = visit.Appointment.Date,
                CreatedAt = visit.CreatedAt,
                Notes = visit.Notes,
                Prescription = visit.Prescription
            })
            .ToListAsync();

        return View(visits);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Details(long id)
    {
        var doctor = await GetCurrentDoctor();
        if (doctor is null)
        {
            return Forbid();
        }

        var patientIds = await GetCurrentDoctorPatientIds(doctor.Id);

        var visit = await _context.Visits
            .AsNoTracking()
            .Include(item => item.Patient)
                .ThenInclude(patient => patient.User)
            .Include(item => item.Doctor)
                .ThenInclude(visitDoctor => visitDoctor.User)
            .Include(item => item.Appointment)
            .Where(item => item.Id == id && patientIds.Contains(item.PatientId))
            .Select(item => new VisitRecordListItemViewModel
            {
                Id = item.Id,
                PatientName = item.Patient.User.FirstName + " " + item.Patient.User.LastName,
                DoctorName = item.Doctor.User.FirstName + " " + item.Doctor.User.LastName,
                AppointmentDate = item.Appointment.Date,
                CreatedAt = item.CreatedAt,
                Notes = item.Notes,
                Prescription = item.Prescription
            })
            .FirstOrDefaultAsync();

        if (visit is null)
        {
            return NotFound();
        }

        return View(visit);
    }

    private async Task<Doctor?> GetCurrentDoctor()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(doctor => doctor.UserId == userId);
    }

    private async Task<List<long>> GetCurrentDoctorPatientIds(long doctorId)
    {
        return await _context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.DoctorId == doctorId)
            .Select(appointment => appointment.PatientId)
            .Distinct()
            .ToListAsync();
    }
}
