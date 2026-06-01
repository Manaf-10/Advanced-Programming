using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;
using Oracle_Health.Services;

namespace Oracle_Health.Controllers;

public class DoctorController : Controller
{
    private readonly ClinicManagementSystemContext _context;
    private readonly IValidationService _validationService;

    public DoctorController(ClinicManagementSystemContext context, IValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
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

    [Authorize(Roles = "Clinic Manager")]
    [HttpGet("/doctors/{id:long}")]
    public async Task<IActionResult> Details(long id)
    {
        var model = await BuildManagerDoctorDetailsViewModel(id);
        if (model is null)
        {
            return NotFound();
        }

        return View("~/Views/Doctor/Details.cshtml", model);
    }

    [Authorize(Roles = "Clinic Manager")]
    [HttpPut("/doctors/{id:long}")]
    [HttpPost("/doctors/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(long id, DoctorProfileEditViewModel model)
    {
        if (id != model.DoctorId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Check the doctor profile details and try again.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var doctor = await _context.Doctors
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (doctor is null)
        {
            return NotFound();
        }

        var normalizedEmail = model.Email.Trim().ToLower();
        var emailExists = await _context.Users.AnyAsync(item =>
            item.Id != doctor.UserId && item.Email.ToLower() == normalizedEmail);

        if (emailExists)
        {
            TempData["ErrorMessage"] = "Another user already uses that email address.";
            return RedirectToAction(nameof(Details), new { id });
        }

        doctor.User.FirstName = model.FirstName.Trim();
        doctor.User.LastName = model.LastName.Trim();
        doctor.User.Email = model.Email.Trim();

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Doctor profile updated.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Clinic Manager")]
    [HttpPost("/doctors/{id:long}/availability")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAvailability(long id, DoctorAvailabilityEditViewModel model)
    {
        if (id != model.DoctorId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid || !model.Date.HasValue || !model.StartTime.HasValue || !model.EndTime.HasValue)
        {
            TempData["ErrorMessage"] = "Check the availability date and time.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var startTime = model.Date.Value.Date.Add(model.StartTime.Value);
        var endTime = model.Date.Value.Date.Add(model.EndTime.Value);

        if (endTime <= startTime)
        {
            TempData["ErrorMessage"] = "Availability end time must be after start time.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var doctorExists = await _context.Doctors.AnyAsync(item => item.Id == id);
        if (!doctorExists)
        {
            return NotFound();
        }

        if (model.IsOnLeave)
        {
            var impactedAppointments = await _validationService.GetImpactedAppointments(id, startTime, endTime);
            if (model.ScheduleId.HasValue)
            {
                var existingSchedule = await _context.Schedules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == model.ScheduleId.Value && item.DoctorId == id);

                if (existingSchedule is not null)
                {
                    impactedAppointments = impactedAppointments
                        .Where(item =>
                            item.Date < endTime
                            && item.Date.AddMinutes(item.DurationMinutes) > startTime)
                        .ToList();
                }
            }

            if (impactedAppointments.Count > 0)
            {
                TempData["ErrorMessage"] = $"There are {impactedAppointments.Count} appointments during this leave period. Reschedule them first.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        Schedule schedule;
        if (model.ScheduleId.HasValue)
        {
            schedule = await _context.Schedules
                .FirstOrDefaultAsync(item => item.Id == model.ScheduleId.Value && item.DoctorId == id)
                ?? new Schedule { DoctorId = id };

            if (schedule.Id == 0)
            {
                _context.Schedules.Add(schedule);
            }
        }
        else
        {
            schedule = new Schedule { DoctorId = id };
            _context.Schedules.Add(schedule);
        }

        schedule.DayOfWeek = startTime.DayOfWeek.ToString();
        schedule.StartTime = startTime;
        schedule.EndTime = endTime;
        schedule.IsOnLeave = model.IsOnLeave;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Doctor availability saved.";

        return RedirectToAction(nameof(Details), new { id });
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

    private async Task<ManagerDoctorDetailsViewModel?> BuildManagerDoctorDetailsViewModel(long doctorId)
    {
        var doctor = await _context.Doctors
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Schedules)
            .Include(item => item.Appointments)
                .ThenInclude(appointment => appointment.Patient)
                .ThenInclude(patient => patient.User)
            .FirstOrDefaultAsync(item => item.Id == doctorId);

        if (doctor is null)
        {
            return null;
        }

        var doctors = await _context.Doctors
            .AsNoTracking()
            .Include(item => item.User)
            .OrderBy(item => item.User.FirstName)
            .ThenBy(item => item.User.LastName)
            .Select(item => new ManagerSelectOptionViewModel
            {
                Id = item.Id,
                Label = "Dr. " + item.User.FirstName + " " + item.User.LastName
            })
            .ToListAsync();

        var patients = await _context.Patients
            .AsNoTracking()
            .Include(item => item.User)
            .OrderBy(item => item.User.FirstName)
            .ThenBy(item => item.User.LastName)
            .Select(item => new ManagerSelectOptionViewModel
            {
                Id = item.Id,
                Label = item.User.FirstName + " " + item.User.LastName + " | Ref: " + item.PatientId
            })
            .ToListAsync();

        var statuses = new[]
        {
            AppointmentStatus.Requested,
            AppointmentStatus.Confirmed,
            AppointmentStatus.CheckedIn,
            AppointmentStatus.InProgress,
            AppointmentStatus.Completed,
            AppointmentStatus.Cancelled,
            AppointmentStatus.Missed
        }
        .Select(status => new ManagerSelectOptionViewModel
        {
            Id = status,
            Label = AppointmentStatus.ToDisplayName(status)
        })
        .ToList();

        return new ManagerDoctorDetailsViewModel
        {
            DoctorId = doctor.Id,
            FullName = "Dr. " + doctor.User.FirstName + " " + doctor.User.LastName,
            Profile = new DoctorProfileEditViewModel
            {
                DoctorId = doctor.Id,
                FirstName = doctor.User.FirstName,
                LastName = doctor.User.LastName,
                Email = doctor.User.Email
            },
            NewAvailability = new DoctorAvailabilityEditViewModel
            {
                DoctorId = doctor.Id,
                Date = DateTime.Today,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            },
            Availability = doctor.Schedules
                .OrderBy(item => item.StartTime)
                .Select(item => new DoctorAvailabilityEditViewModel
                {
                    ScheduleId = item.Id,
                    DoctorId = doctor.Id,
                    Date = item.StartTime.Date,
                    StartTime = item.StartTime.TimeOfDay,
                    EndTime = item.EndTime.TimeOfDay,
                    IsOnLeave = item.IsOnLeave == true
                })
                .ToList(),
            Appointments = doctor.Appointments
                .OrderBy(item => item.Date)
                .Select(item => new ManagerAppointmentEditViewModel
                {
                    AppointmentId = item.Id,
                    PatientId = item.PatientId,
                    DoctorId = item.DoctorId,
                    AppointmentDate = item.Date.Date,
                    AppointmentTime = item.Date.TimeOfDay,
                    DurationMinutes = item.DurationMinutes,
                    Status = item.Status,
                    PatientName = item.Patient.User.FirstName + " " + item.Patient.User.LastName,
                    DoctorName = doctor.User.FirstName + " " + doctor.User.LastName
                })
                .ToList(),
            Doctors = doctors,
            Patients = patients,
            Statuses = statuses
        };
    }
}
