using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health.Controllers;

public class LookupController : Controller
{
    private readonly ClinicManagementSystemContext _context;

    public LookupController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new LookupViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LookupViewModel form)
    {
        var viewModel = new LookupViewModel
        {
            PatientReference = form.PatientReference,
            Searched = true
        };

        if (form.PatientReference == null)
        {
            ModelState.AddModelError(nameof(form.PatientReference), "Please enter a patient reference number.");
            return View(viewModel);
        }

        // Find patient by their reference number (PatientId)
        var patient = await _context.Patients
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PatientId == form.PatientReference.Value);

        if (patient == null)
        {
            ModelState.AddModelError(nameof(form.PatientReference), "No patient found with that reference number.");
            return View(viewModel);
        }

        // Only show upcoming appointments (not completed/cancelled/missed)
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.PatientId == patient.Id &&
                a.Date >= DateTime.Now &&
                a.Status != AppointmentStatus.Completed &&
                a.Status != AppointmentStatus.Cancelled &&
                a.Status != AppointmentStatus.Missed)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor).ThenInclude(d => d.Specializations)
            .OrderBy(a => a.Date)
            .Select(a => new LookupAppointmentViewModel
            {
                DoctorName = "Dr. " + a.Doctor.User.FirstName + " " + a.Doctor.User.LastName,
                Specialization = a.Doctor.Specializations.Select(s => s.Name).FirstOrDefault() ?? "General",
                Date = a.Date,
                Status = AppointmentStatus.ToDisplayName(a.Status)
            })
            .ToListAsync();

        viewModel.PatientName = patient.User.FirstName + " " + patient.User.LastName;
        viewModel.Appointments = appointments;

        return View(viewModel);
    }
}