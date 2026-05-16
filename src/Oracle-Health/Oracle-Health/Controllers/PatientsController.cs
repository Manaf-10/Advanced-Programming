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

    [Authorize(Roles = "Admin,Reception,Doctor")]
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
