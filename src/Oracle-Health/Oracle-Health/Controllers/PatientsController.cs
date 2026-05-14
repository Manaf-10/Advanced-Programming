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

    public async Task<IActionResult> List()
    {
        var patients = await _context.Patients
            .AsNoTracking()
            .Include(patient => patient.User)
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

        return View("~/Views/Patients/List.cshtml", patients);
    }
}
