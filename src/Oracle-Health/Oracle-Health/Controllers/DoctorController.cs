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
    /**
     * The Index action retrieves a list of doctors from the database, including their associated 
     * user information, specializations, schedules, and appointments. It then maps this data to 
     * a list of DoctorListItemViewModel instances, which are passed to the view for rendering. 
     * The doctors are ordered by their first and last names for easier navigation.
     * */
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

    /**
     * The MyAppointments action retrieves the currently logged-in doctor's appointments from the database. 
     * It first checks if the user is authenticated and has the "Doctor" role. Then, it finds the doctor 
     * record associated with the logged-in user and retrieves their appointments, including patient information. 
     * The appointments are mapped to a list of AppointmentListItemViewModel instances, which are passed to the view for rendering.
     * */
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
    /**
     * The MyAvailability action retrieves the currently logged-in doctor's availability schedule for a specified week. 
     * It first checks if the user is authenticated and has the "Doctor" role. Then, it finds the doctor record 
     * associated with the logged-in user and retrieves their schedule blocks and appointments for the selected week. 
     * The schedule blocks and appointments are combined into a list of DoctorAvailabilityBlockViewModel instances, which are grouped by day and passed to the view for rendering.
     * */
    [Authorize(Roles = "Doctor")]
    [HttpGet("/doctors/me/availability")]
    public async Task<IActionResult> MyAvailability(DateTime? weekStart)
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

        var selectedWeekStart = GetWeekStart((weekStart ?? DateTime.Today).Date);
        var selectedWeekEnd = selectedWeekStart.AddDays(7);

        var scheduleBlocks = await _context.Schedules
            .AsNoTracking()
            .Where(schedule =>
                schedule.DoctorId == doctor.Id
                && schedule.StartTime >= selectedWeekStart
                && schedule.StartTime < selectedWeekEnd)
            .Select(schedule => new DoctorAvailabilityBlockViewModel
            {
                Title = schedule.IsOnLeave == true ? "Off / Leave" : "Available",
                Detail = schedule.IsOnLeave == true ? "Not accepting appointments" : "Working hours",
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Type = schedule.IsOnLeave == true ? "off" : "working"
            })
            .ToListAsync();

        var appointmentBlocks = await _context.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Patient)
                .ThenInclude(patient => patient.User)
            .Where(appointment =>
                appointment.DoctorId == doctor.Id
                && appointment.Date >= selectedWeekStart
                && appointment.Date < selectedWeekEnd)
            .Select(appointment => new DoctorAvailabilityBlockViewModel
            {
                Title = appointment.Patient.User.FirstName + " " + appointment.Patient.User.LastName,
                Detail = AppointmentStatus.ToDisplayName(appointment.Status),
                StartTime = appointment.Date,
                EndTime = appointment.Date.AddMinutes(appointment.DurationMinutes),
                Type = "appointment"
            })
            .ToListAsync();

        var blocks = scheduleBlocks
            .Concat(appointmentBlocks)
            .OrderBy(block => block.StartTime)
            .ThenBy(block => block.Type)
            .ToList();

        var days = Enumerable.Range(0, 7)
            .Select(dayOffset =>
            {
                var date = selectedWeekStart.AddDays(dayOffset);
                return new DoctorAvailabilityDayViewModel
                {
                    Date = date,
                    Blocks = blocks
                        .Where(block => block.StartTime.Date == date.Date)
                        .OrderBy(block => block.StartTime)
                        .ToList()
                };
            })
            .ToList();

        return View("~/Views/Doctor/Availability.cshtml", new DoctorAvailabilityViewModel
        {
            WeekStart = selectedWeekStart,
            Days = days
        });
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var daysSinceSunday = (int)date.DayOfWeek;
        return date.AddDays(-daysSinceSunday).Date;
    }
}
