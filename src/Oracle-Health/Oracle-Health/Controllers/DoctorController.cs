using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health.Controllers;

public class DoctorController : Controller
{
    private readonly ClinicManagementSystemContext _context;

    public DoctorController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var doctors = await _context.Doctors
            .AsNoTracking()
            .Include(doctor => doctor.User)
            .Include(doctor => doctor.Specializations)
            .Include(doctor => doctor.Schedules)
            .Include(doctor => doctor.Appointments)
            .OrderBy(doctor => doctor.User.FirstName)
            .ThenBy(doctor => doctor.User.LastName)
            .Select(doctor => new DoctorListItemViewModel
            {
                Id = doctor.Id,
                FullName = doctor.User.FirstName + " " + doctor.User.LastName,
                Email = doctor.User.Email,
                Specializations = doctor.Specializations
                    .OrderBy(specialization => specialization.Name)
                    .Select(specialization => specialization.Name)
                    .ToList(),
                UpcomingAppointments = doctor.Appointments.Count(appointment => appointment.Date >= today),
                Schedule = doctor.Schedules
                    .OrderBy(schedule => schedule.DayOfWeek)
                    .ThenBy(schedule => schedule.StartTime)
                    .Select(schedule => new DoctorScheduleItemViewModel
                    {
                        DayOfWeek = schedule.DayOfWeek,
                        StartTime = schedule.StartTime,
                        EndTime = schedule.EndTime,
                        IsOnLeave = schedule.IsOnLeave == true
                    })
                    .ToList()
            })
            .ToListAsync();

        return View(new DoctorIndexViewModel { Doctors = doctors });
    }

    [Authorize(Roles = "Doctor")]
    [HttpGet("/doctors/me/appointments")]
    public async Task<IActionResult> MyAppointments()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        var doctor = await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId);

        if (doctor is null)
        {
            return Forbid();
        }

        var appointmentRecords = await _context.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Patient)
                .ThenInclude(patient => patient.User)
            .Where(appointment => appointment.DoctorId == doctor.Id)
            .OrderBy(appointment => appointment.Date)
            .ToListAsync();

        var appointments = appointmentRecords
            .Select(appointment => new AppointmentListItemViewModel
            {
                Id = appointment.Id,
                PatientName = appointment.Patient.User.FirstName + " " + appointment.Patient.User.LastName,
                DoctorName = string.Empty,
                Date = appointment.Date,
                Status = AppointmentStatus.ToDisplayName(appointment.Status),
                CanOpenDetails = appointment.Status == AppointmentStatus.InProgress,
                StatusActions = appointment.Status == AppointmentStatus.CheckedIn
                    ? new List<AppointmentStatusActionViewModel>
                    {
                        new()
                        {
                            Status = AppointmentStatus.InProgress,
                            Label = AppointmentStatus.ToActionLabel(AppointmentStatus.InProgress)
                        }
                    }
                    : new List<AppointmentStatusActionViewModel>()
            })
            .ToList();

        ViewData["Title"] = "My Appointments";
        return View("~/Views/Appointment/Index.cshtml", appointments);
    }
}
