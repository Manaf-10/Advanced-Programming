using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize]
    public async Task<IActionResult> Index()
    {
        IQueryable<Appointment> appointmentQuery = _context.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Patient)
                .ThenInclude(patient => patient.User)
            .Include(appointment => appointment.Doctor)
                .ThenInclude(doctor => doctor.User);

        if (!User.IsInRole("Admin") && !User.IsInRole("Reception"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Forbid();
            }

            if (User.IsInRole("Doctor"))
            {
                appointmentQuery = appointmentQuery
                    .Where(appointment => appointment.Doctor.UserId == userId);
            }
            else if (User.IsInRole("Patient"))
            {
                appointmentQuery = appointmentQuery
                    .Where(appointment => appointment.Patient.UserId == userId);
            }
            else
            {
                return Forbid();
            }
        }

        var appointmentRecords = await appointmentQuery
            .OrderBy(appointment => appointment.Date)
            .ToListAsync();

        var appointments = appointmentRecords
            .Select(appointment => new AppointmentListItemViewModel
            {
                Id = appointment.Id,
                PatientName = appointment.Patient.User.FirstName + " " + appointment.Patient.User.LastName,
                DoctorName = appointment.Doctor.User.FirstName + " " + appointment.Doctor.User.LastName,
                Date = appointment.Date,
                Status = AppointmentStatus.ToDisplayName(appointment.Status),
                CanOpenDetails = CanCurrentUserOpenVisitForm(appointment),
                StatusActions = GetVisibleStatusActions(appointment)
            })
            .ToList();

        return View("~/Views/Appointment/Index.cshtml", appointments);
    }

    [Authorize]
    public async Task<IActionResult> Details(long id)
    {
        var appointmentQuery = _context.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Patient)
                .ThenInclude(patient => patient.User)
            .Include(appointment => appointment.Doctor)
                .ThenInclude(doctor => doctor.User)
            .OrderBy(appointment => appointment.Date);

        var appointment = await appointmentQuery.FirstOrDefaultAsync(item => item.Id == id);

        if (appointment is null)
        {
            return RedirectToAction("Index");
        }

        if (!CanCurrentUserViewAppointment(appointment))
        {
            return Forbid();
        }

        if (!CanCurrentUserOpenVisitForm(appointment))
        {
            return Forbid();
        }

        var model = new AppointmentVisitFormViewModel
        {
            AppointmentId = appointment.Id,
            PatientName = $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}",
            DoctorName = $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
            AppointmentDate = appointment.Date
        };

        return View("~/Views/Appointment/Details.cshtml", model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long id, int status)
    {
        var appointment = await _context.Appointments
            .Include(item => item.Doctor)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (appointment is null)
        {
            return NotFound();
        }

        if (!AppointmentStatus.CanTransition(appointment.Status, status))
        {
            TempData["ErrorMessage"] = "That appointment status change is not allowed.";
            return RedirectToAction("Index");
        }

        if (!CanCurrentUserSetStatus(appointment, status))
        {
            return Forbid();
        }

        appointment.Status = status;
        await _context.SaveChangesAsync();
        await BroadcastStatusChange(appointment);

        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDetails(AppointmentVisitFormViewModel model)
    {
        var appointment = await _context.Appointments
            .Include(item => item.Doctor)
            .Include(item => item.Visit)
            .FirstOrDefaultAsync(item => item.Id == model.AppointmentId);

        if (appointment is null)
        {
            return NotFound();
        }

        if (!CanCurrentDoctorCompleteVisit(appointment))
        {
            return Forbid();
        }

        if (appointment.Status != AppointmentStatus.InProgress)
        {
            ModelState.AddModelError(string.Empty, "Start the appointment before completing the visit record.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateAppointmentDetails(model, appointment.Id);
            return View("~/Views/Appointment/Details.cshtml", model);
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
        await BroadcastStatusChange(appointment);

        return RedirectToAction("Index");
    }

    private List<AppointmentStatusActionViewModel> GetVisibleStatusActions(Appointment appointment)
    {
        return AppointmentStatus.GetAllowedNextStatuses(appointment.Status)
            .Where(nextStatus => CanCurrentUserSetStatus(appointment, nextStatus))
            .Select(nextStatus => new AppointmentStatusActionViewModel
            {
                Status = nextStatus,
                Label = AppointmentStatus.ToActionLabel(nextStatus)
            })
            .ToList();
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

    private bool CanCurrentUserSetStatus(Appointment appointment, int nextStatus)
    {
        if (User.IsInRole("Admin"))
        {
            return nextStatus is AppointmentStatus.Confirmed
                or AppointmentStatus.Cancelled
                or AppointmentStatus.Missed;
        }

        if (User.IsInRole("Reception"))
        {
            return nextStatus is AppointmentStatus.Confirmed
                or AppointmentStatus.CheckedIn
                or AppointmentStatus.Cancelled
                or AppointmentStatus.Missed;
        }

        if (User.IsInRole("Doctor") && nextStatus == AppointmentStatus.InProgress)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(userIdClaim, out var userId) && appointment.Doctor.UserId == userId;
        }

        return false;
    }

    private bool CanCurrentDoctorCompleteVisit(Appointment appointment)
    {
        if (!User.IsInRole("Doctor"))
        {
            return false;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(userIdClaim, out var userId) && appointment.Doctor.UserId == userId;
    }

    private bool CanCurrentUserViewAppointment(Appointment appointment)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Reception"))
        {
            return true;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return false;
        }

        if (User.IsInRole("Doctor"))
        {
            return appointment.Doctor.UserId == userId;
        }

        if (User.IsInRole("Patient"))
        {
            return appointment.Patient.UserId == userId;
        }

        return false;
    }

    private bool CanCurrentUserOpenVisitForm(Appointment appointment)
    {
        return appointment.Status == AppointmentStatus.InProgress
            && CanCurrentDoctorCompleteVisit(appointment);
    }

    private async Task BroadcastStatusChange(Appointment appointment)
    {
        await _appointmentHub.Clients.All.SendAsync(
            "AppointmentStatusChanged",
            appointment.Id,
            AppointmentStatus.ToDisplayName(appointment.Status));
    }
}
