using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Hubs;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health.Controllers;

public class AppointmentsController : Controller
{
    private readonly ClinicManagementSystemContext _context;
    private readonly IHubContext<AppointmentHub> _appointmentHub;

    public AppointmentsController(
        ClinicManagementSystemContext context,
        IHubContext<AppointmentHub> appointmentHub)
    {
        _context = context;
        _appointmentHub = appointmentHub;
    }

    public async Task<IActionResult> Index(long? id)
    {
        var appointmentQuery = _context.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Patient)
                .ThenInclude(patient => patient.User)
            .Include(appointment => appointment.Doctor)
                .ThenInclude(doctor => doctor.User)
            .OrderBy(appointment => appointment.Date);

        var appointment = id.HasValue
            ? await appointmentQuery.FirstOrDefaultAsync(item => item.Id == id.Value)
            : await appointmentQuery.FirstOrDefaultAsync();

        if (appointment is null)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new AppointmentVisitFormViewModel
        {
            AppointmentId = appointment.Id,
            PatientName = $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}",
            DoctorName = $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
            AppointmentDate = appointment.Date
        };

        return View("~/Views/Appointment/Index.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDetails(AppointmentVisitFormViewModel model)
    {
        var appointment = await _context.Appointments
            .Include(item => item.Visit)
            .FirstOrDefaultAsync(item => item.Id == model.AppointmentId);

        if (appointment is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateAppointmentDetails(model, appointment.Id);
            return View("~/Views/Appointment/Index.cshtml", model);
        }

        var notes = string.IsNullOrWhiteSpace(model.Notes)
            ? $"Diagnosis: {model.Diagnosis}"
            : $"Diagnosis: {model.Diagnosis}{Environment.NewLine}Notes: {model.Notes}";

        if (appointment.Visit is null)
        {
            _context.Visits.Add(new Visit
            {
                AppointmentId = appointment.Id,
                DoctorId = appointment.DoctorId,
                PatientId = appointment.PatientId,
                Notes = notes,
                Prescription = model.Prescription,
                CreatedAt = DateTime.Now
            });
        }
        else
        {
            appointment.Visit.Notes = notes;
            appointment.Visit.Prescription = model.Prescription;
            appointment.Visit.CreatedAt ??= DateTime.Now;
        }

        appointment.Status = AppointmentStatus.Completed;
        await _context.SaveChangesAsync();
        await _appointmentHub.Clients.All.SendAsync(
            "AppointmentStatusChanged",
            appointment.Id,
            AppointmentStatus.ToDisplayName(appointment.Status));

        return RedirectToAction("Index", "Home");
    }

    private async Task PopulateAppointmentDetails(AppointmentVisitFormViewModel model, long appointmentId)
    {
        var details = await _context.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Patient)
                .ThenInclude(patient => patient.User)
            .Include(appointment => appointment.Doctor)
                .ThenInclude(doctor => doctor.User)
            .FirstOrDefaultAsync(appointment => appointment.Id == appointmentId);

        if (details is null)
        {
            return;
        }

        model.PatientName = $"{details.Patient.User.FirstName} {details.Patient.User.LastName}";
        model.DoctorName = $"{details.Doctor.User.FirstName} {details.Doctor.User.LastName}";
        model.AppointmentDate = details.Date;
    }
}
