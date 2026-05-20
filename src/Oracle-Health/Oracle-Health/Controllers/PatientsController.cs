using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health.Controllers;

public class PatientsController : Controller
{
    private readonly ClinicManagementSystemContext _context;

    public PatientsController(ClinicManagementSystemContext context)
    {
        _context = context;
    }


    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> History()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return RedirectToAction("Login", "Account");

        var userId = long.Parse(userIdClaim);

        var patient = await _context.Patients
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null) return NotFound();

        var visits = await _context.Visits
            .AsNoTracking()
            .Where(v => v.PatientId == patient.Id)
            .Include(v => v.Doctor)
                .ThenInclude(d => d.User)
            .Include(v => v.Doctor)
                .ThenInclude(d => d.Specializations)
            .Include(v => v.Appointment)
            .OrderByDescending(v => v.Appointment.Date)
            .Select(v => new PatientHistoryItemViewModel
            {
                VisitId = v.Id,
                DoctorName = "Dr. " + v.Doctor.User.FirstName + " " + v.Doctor.User.LastName,
                Specialization = v.Doctor.Specializations
                                   .Select(s => s.Name)
                                   .FirstOrDefault() ?? "General",
                VisitDate = v.Appointment.Date,
                Notes = v.Notes,
                Prescription = v.Prescription
            })
            .ToListAsync();

        var viewModel = new PatientHistoryViewModel
        {
            PatientName = patient.User.FirstName + " " + patient.User.LastName,
            Visits = visits
        };

        return View("~/Views/Patients/History.cshtml", viewModel);
    }

    [Route("patients/prescriptions")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> Prescriptions()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return RedirectToAction("Login", "Account");

        var userId = long.Parse(userIdClaim);

        var patient = await _context.Patients
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null) return NotFound();

        var prescriptions = await _context.Visits
            .AsNoTracking()
            .Where(v => v.PatientId == patient.Id && v.Prescription != null && v.Prescription != "")
            .Include(v => v.Doctor).ThenInclude(d => d.User)
            .Include(v => v.Doctor).ThenInclude(d => d.Specializations)
            .Include(v => v.Appointment)
            .OrderByDescending(v => v.Appointment.Date)
            .Select(v => new PrescriptionItemViewModel
            {
                VisitId = v.Id,
                DoctorName = "Dr. " + v.Doctor.User.FirstName + " " + v.Doctor.User.LastName,
                Specialization = v.Doctor.Specializations.Select(s => s.Name).FirstOrDefault() ?? "General",
                VisitDate = v.Appointment.Date,
                Prescription = v.Prescription!,
                Notes = v.Notes
            })
            .ToListAsync();

        var viewModel = new PatientPrescriptionsViewModel
        {
            PatientName = patient.User.FirstName + " " + patient.User.LastName,
            Prescriptions = prescriptions
        };

        return View("~/Views/Patients/Prescriptions.cshtml", viewModel);
    }

    public async Task<IActionResult> List(string? searchTerm)
    {
        IQueryable<Patient> patientQuery = _context.Patients
            .AsNoTracking()
            .Include(patient => patient.User);

        if (User.IsInRole("Doctor"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Forbid();
            }

            patientQuery = patientQuery
                .Where(patient => patient.Appointments.Any(appointment => appointment.Doctor.UserId == userId));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim();
            if (long.TryParse(normalizedSearchTerm, out var numericSearchTerm))
            {
                patientQuery = patientQuery.Where(patient =>
                    patient.User.FirstName.Contains(normalizedSearchTerm)
                    || patient.User.LastName.Contains(normalizedSearchTerm)
                    || patient.Cpr == numericSearchTerm
                    || patient.PatientId == numericSearchTerm);
            }
            else
            {
                patientQuery = patientQuery.Where(patient =>
                    patient.User.FirstName.Contains(normalizedSearchTerm)
                    || patient.User.LastName.Contains(normalizedSearchTerm));
            }
        }

        var patients = await patientQuery
            .OrderBy(patient => patient.User.FirstName)
            .ThenBy(patient => patient.User.LastName)
            .Select(patient => new PatientCardViewModel
            {
                Id = patient.Id,
                PatientReference = patient.PatientId,
                Cpr = patient.Cpr,
                FullName = patient.User.FirstName + " " + patient.User.LastName
            })
            .ToListAsync();

        ViewData["SearchTerm"] = searchTerm?.Trim() ?? string.Empty;
        return View("~/Views/Patients/List.cshtml", patients);
    }
}
