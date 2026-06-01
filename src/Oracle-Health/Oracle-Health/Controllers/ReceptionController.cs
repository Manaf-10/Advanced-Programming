using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health.Controllers;

public class ReceptionController : Controller
{
    private readonly ClinicManagementSystemContext _context;

    public ReceptionController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin,Reception,Patient,Doctor")]
    [HttpGet("/appointments/book")]
    public async Task<IActionResult> Book()
    {
        var bookingScope = await ResolveBookingScopeAsync();
        if (!bookingScope.IsValid)
        {
            return Forbid();
        }

        var model = await BuildBookingViewModelAsync(new AppointmentBookingViewModel
        {
            AppointmentDate = GetSuggestedBookingDate(),
            AppointmentTime = new TimeSpan(9, 0, 0),
            DurationMinutes = 30
        }, bookingScope);

        return View("~/Views/Reception/Book.cshtml", model);
    }

    [Authorize(Roles = "Admin,Reception,Patient,Doctor")]
    [HttpPost("/appointments/book")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(AppointmentBookingViewModel model)
    {
        var bookingScope = await ResolveBookingScopeAsync();
        if (!bookingScope.IsValid)
        {
            return Forbid();
        }

        if (bookingScope.FixedPatientId.HasValue)
        {
            model.SelectedPatientId = bookingScope.FixedPatientId.Value;
        }

        if (bookingScope.FixedDoctorId.HasValue)
        {
            model.SelectedDoctorId = bookingScope.FixedDoctorId.Value;
        }

        if (!model.SelectedPatientId.HasValue)
        {
            ModelState.AddModelError(nameof(model.SelectedPatientId), "Select a patient before creating the appointment.");
        }

        if (!model.SelectedDoctorId.HasValue)
        {
            ModelState.AddModelError(nameof(model.SelectedDoctorId), "Select a doctor.");
        }

        if (!model.AppointmentDate.HasValue)
        {
            ModelState.AddModelError(nameof(model.AppointmentDate), "Choose an appointment date.");
        }

        if (!model.AppointmentTime.HasValue)
        {
            ModelState.AddModelError(nameof(model.AppointmentTime), "Choose an appointment time.");
        }

        if (model.SelectedPatientId.HasValue
            && bookingScope.AllowedPatientIds is not null
            && !bookingScope.AllowedPatientIds.Contains(model.SelectedPatientId.Value))
        {
            ModelState.AddModelError(nameof(model.SelectedPatientId), "Select one of your patients for the follow-up appointment.");
        }

        if (!ModelState.IsValid)
        {
            model = await BuildBookingViewModelAsync(model, bookingScope);
            return View("~/Views/Reception/Book.cshtml", model);
        }

        var selectedDoctorId = model.SelectedDoctorId;
        var selectedPatientId = model.SelectedPatientId;

        var appointmentStart = model.AppointmentDate!.Value.Date.Add(model.AppointmentTime!.Value);
        var appointmentEnd = appointmentStart.AddMinutes(model.DurationMinutes);

        if (appointmentStart < DateTime.Now.AddMinutes(-1))
        {
            ModelState.AddModelError(nameof(model.AppointmentDate), "Appointments must be booked for a current or future slot.");
        }

        var doctor = await _context.Doctors
            .Include(item => item.User)
            .Include(item => item.Specializations)
            .FirstOrDefaultAsync(item => item.Id == selectedDoctorId);

        if (doctor is null)
        {
            ModelState.AddModelError(nameof(model.SelectedDoctorId), "The selected doctor could not be found.");
        }

        var patient = await _context.Patients
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == selectedPatientId);

        if (patient is null)
        {
            ModelState.AddModelError(nameof(model.SelectedPatientId), "The selected patient could not be found.");
        }

        if (doctor is not null && model.SelectedSpecializationId.HasValue)
        {
            var doctorHasSpecialization = doctor.Specializations.Any(item => item.Id == model.SelectedSpecializationId.Value);
            if (!doctorHasSpecialization)
            {
                ModelState.AddModelError(nameof(model.SelectedDoctorId), "The selected doctor does not match the chosen specialization.");
            }
        }

        if (!ModelState.IsValid || doctor is null || patient is null)
        {
            model = await BuildBookingViewModelAsync(model, bookingScope);
            return View("~/Views/Reception/Book.cshtml", model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        if (!await HasWorkingScheduleAsync(doctor.Id, appointmentStart, appointmentEnd))
        {
            ModelState.AddModelError(nameof(model.AppointmentTime), "The doctor is not available during the selected time slot.");
        }

        if (await HasOverlappingAppointmentAsync(doctor.Id, appointmentStart, appointmentEnd))
        {
            ModelState.AddModelError(nameof(model.AppointmentTime), "This slot is already booked. Choose another time.");
        }

        if (!ModelState.IsValid)
        {
            model = await BuildBookingViewModelAsync(model, bookingScope);
            return View("~/Views/Reception/Book.cshtml", model);
        }

        var initialStatus = User.IsInRole("Patient")
            ? AppointmentStatus.Requested
            : AppointmentStatus.Confirmed;

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Date = appointmentStart,
            DurationMinutes = model.DurationMinutes,
            Status = initialStatus
        };

        _context.Appointments.Add(appointment);

        var doctorName = $"Dr. {doctor.User.FirstName} {doctor.User.LastName}";
        var patientName = $"{patient.User.FirstName} {patient.User.LastName}";
        var slotText = appointmentStart.ToString("dddd, dd MMM yyyy | hh:mm tt");

        _context.Notifications.Add(new Notification
        {
            UserId = patient.UserId,
            Message = User.IsInRole("Patient")
                ? $"Your appointment request with {doctorName} was submitted for {slotText}."
                : $"Your appointment with {doctorName} is confirmed for {slotText}.",
            CreatedAt = DateTime.Now
        });

        _context.Notifications.Add(new Notification
        {
            UserId = doctor.UserId,
            Message = $"{patientName} has a {(initialStatus == AppointmentStatus.Confirmed ? "confirmed" : "requested")} appointment for {slotText}.",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = User.IsInRole("Patient")
            ? "Appointment request submitted successfully."
            : "Appointment booked successfully.";

        return RedirectToAction("Index", "Appointments");
    }

    [Authorize(Roles = "Admin,Reception,Patient,Doctor")]
    [HttpGet("/appointments/availability")]
    public async Task<IActionResult> Availability(long? doctorId, DateTime? date, int durationMinutes = 30)
    {
        var bookingScope = await ResolveBookingScopeAsync();
        if (!bookingScope.IsValid)
        {
            return Forbid();
        }

        if (bookingScope.FixedDoctorId.HasValue)
        {
            doctorId = bookingScope.FixedDoctorId.Value;
        }

        if (!doctorId.HasValue || !date.HasValue || durationMinutes is < 15 or > 120)
        {
            return BadRequest();
        }

        var doctorExists = await _context.Doctors
            .AsNoTracking()
            .AnyAsync(item => item.Id == doctorId.Value);

        if (!doctorExists)
        {
            return NotFound();
        }

        var slots = await BuildAvailabilitySlotsAsync(doctorId.Value, date.Value.Date, durationMinutes);

        return Json(new
        {
            DoctorId = doctorId.Value,
            Date = date.Value.ToString("yyyy-MM-dd"),
            DurationMinutes = durationMinutes,
            Slots = slots
        });
    }

    [Authorize(Roles = "Admin,Reception,Doctor")]
    [HttpGet("/appointments/live")]
    public async Task<IActionResult> LiveBoard()
    {
        var boardDate = DateTime.Today;
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(item => item.Patient)
                .ThenInclude(patient => patient.User)
            .Include(item => item.Doctor)
                .ThenInclude(doctor => doctor.User)
            .Include(item => item.Doctor)
                .ThenInclude(doctor => doctor.Specializations)
            .Where(item => item.Date.Date == boardDate)
            .OrderBy(item => item.Date)
            .ToListAsync();

        var model = new LiveBoardViewModel
        {
            BoardDate = boardDate,
            Items = appointments.Select(item => new LiveBoardAppointmentItemViewModel
            {
                AppointmentId = item.Id,
                PatientName = $"{item.Patient.User.FirstName} {item.Patient.User.LastName}",
                DoctorName = $"Dr. {item.Doctor.User.FirstName} {item.Doctor.User.LastName}",
                Time = item.Date,
                EndTime = item.Date.AddMinutes(item.DurationMinutes),
                Status = AppointmentStatus.ToDisplayName(item.Status),
                Specializations = item.Doctor.Specializations.Select(spec => spec.Name).OrderBy(name => name).ToList()
            }).ToList()
        };

        return View("~/Views/Reception/LiveBoard.cshtml", model);
    }

    private async Task<bool> HasWorkingScheduleAsync(long doctorId, DateTime appointmentStart, DateTime appointmentEnd)
    {
        var schedules = await _context.Schedules
            .AsNoTracking()
            .Where(item =>
                item.DoctorId == doctorId
                && item.StartTime < appointmentEnd
                && item.EndTime > appointmentStart)
            .ToListAsync();

        var hasLeaveConflict = schedules.Any(item =>
            item.IsOnLeave == true
            && item.StartTime < appointmentEnd
            && item.EndTime > appointmentStart);

        if (hasLeaveConflict)
        {
            return false;
        }

        return schedules.Any(item =>
            item.IsOnLeave != true
            && item.StartTime <= appointmentStart
            && item.EndTime >= appointmentEnd);
    }

    private Task<bool> HasOverlappingAppointmentAsync(long doctorId, DateTime appointmentStart, DateTime appointmentEnd)
    {
        return _context.Appointments
            .AsNoTracking()
            .Where(item =>
                item.DoctorId == doctorId
                && item.Status != AppointmentStatus.Cancelled
                && item.Status != AppointmentStatus.Missed)
            .AnyAsync(item =>
                item.Date < appointmentEnd
                && item.Date.AddMinutes(item.DurationMinutes) > appointmentStart);
    }

    private async Task<List<AvailabilitySlotResult>> BuildAvailabilitySlotsAsync(
        long doctorId,
        DateTime selectedDate,
        int durationMinutes)
    {
        var dayStart = selectedDate.Date;
        var dayEnd = dayStart.AddDays(1);

        var schedules = await _context.Schedules
            .AsNoTracking()
            .Where(item =>
                item.DoctorId == doctorId
                && item.StartTime < dayEnd
                && item.EndTime > dayStart)
            .ToListAsync();

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(item =>
                item.DoctorId == doctorId
                && item.Status != AppointmentStatus.Cancelled
                && item.Status != AppointmentStatus.Missed
                && item.Date < dayEnd
                && item.Date.AddMinutes(item.DurationMinutes) > dayStart)
            .ToListAsync();

        var displayStart = dayStart.AddHours(8);
        var displayEnd = dayStart.AddHours(18);

        if (schedules.Any())
        {
            var earliestSchedule = schedules.Min(item => item.StartTime);
            var latestSchedule = schedules.Max(item => item.EndTime);
            displayStart = earliestSchedule < displayStart ? earliestSchedule : displayStart;
            displayEnd = latestSchedule > displayEnd ? latestSchedule : displayEnd;
        }

        var slots = new List<AvailabilitySlotResult>();
        for (var slotStart = displayStart; slotStart < displayEnd; slotStart = slotStart.AddMinutes(15))
        {
            var slotEnd = slotStart.AddMinutes(durationMinutes);
            var isWorking = schedules.Any(item =>
                item.IsOnLeave != true
                && item.StartTime <= slotStart
                && item.EndTime >= slotEnd);
            var hasLeaveConflict = schedules.Any(item =>
                item.IsOnLeave == true
                && item.StartTime < slotEnd
                && item.EndTime > slotStart);
            var hasAppointmentConflict = appointments.Any(item =>
                item.Date < slotEnd
                && item.Date.AddMinutes(item.DurationMinutes) > slotStart);

            var status = hasAppointmentConflict
                ? "busy"
                : !isWorking || hasLeaveConflict
                    ? "not-working"
                    : "available";

            slots.Add(new AvailabilitySlotResult
            {
                Time = slotStart.ToString("hh:mm tt"),
                Value = slotStart.ToString("HH:mm"),
                Status = status,
                StatusLabel = status switch
                {
                    "available" => "Available",
                    "busy" => "Busy",
                    _ => "Not working"
                }
            });
        }

        return slots;
    }

    private async Task<AppointmentBookingViewModel> BuildBookingViewModelAsync(
        AppointmentBookingViewModel model,
        BookingScopeResult bookingScope)
    {
        model.CanSelectPatient = !bookingScope.FixedPatientId.HasValue;
        model.CanSelectDoctor = !bookingScope.FixedDoctorId.HasValue;

        var specializations = await _context.Specializations
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync();

        model.Specializations = specializations
            .Select(item => new AppointmentBookingSelectOptionViewModel
            {
                Id = item.Id,
                Label = item.Name
            })
            .ToList();

        IQueryable<Doctor> doctorQuery = _context.Doctors
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Specializations);

        if (bookingScope.FixedDoctorId.HasValue)
        {
            doctorQuery = doctorQuery.Where(item => item.Id == bookingScope.FixedDoctorId.Value);
        }

        var doctors = await doctorQuery
            .OrderBy(item => item.User.FirstName)
            .ThenBy(item => item.User.LastName)
            .ToListAsync();

        model.Doctors = doctors
            .Select(item => new AppointmentBookingDoctorOptionViewModel
            {
                Id = item.Id,
                FullName = $"Dr. {item.User.FirstName} {item.User.LastName}",
                SpecializationIds = item.Specializations.Select(spec => spec.Id).OrderBy(id => id).ToList(),
                SpecializationSummary = item.Specializations.Any()
                    ? string.Join(", ", item.Specializations.Select(spec => spec.Name).OrderBy(name => name))
                    : "General"
            })
            .ToList();

        if (model.CanSelectPatient)
        {
            IQueryable<Patient> patientQuery = _context.Patients
                .AsNoTracking()
                .Include(item => item.User);

            if (bookingScope.AllowedPatientIds is not null)
            {
                patientQuery = patientQuery.Where(item => bookingScope.AllowedPatientIds.Contains(item.Id));
            }

            var patients = await patientQuery
                .OrderBy(item => item.User.FirstName)
                .ThenBy(item => item.User.LastName)
                .ToListAsync();

            model.Patients = patients
                .Select(item => new AppointmentBookingPatientOptionViewModel
                {
                    Id = item.Id,
                    FullName = $"{item.User.FirstName} {item.User.LastName}",
                    PatientReference = item.PatientId,
                    Cpr = item.Cpr
                })
                .ToList();
        }
        else
        {
            model.Patients = new List<AppointmentBookingPatientOptionViewModel>();
        }

        if (bookingScope.FixedPatientId.HasValue)
        {
            model.SelectedPatientId = bookingScope.FixedPatientId.Value;
        }

        if (bookingScope.FixedDoctorId.HasValue)
        {
            model.SelectedDoctorId = bookingScope.FixedDoctorId.Value;
        }

        return model;
    }

    private async Task<BookingScopeResult> ResolveBookingScopeAsync()
    {
        if (User.IsInRole("Admin") || User.IsInRole("Reception"))
        {
            return new BookingScopeResult { IsValid = true };
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return new BookingScopeResult();
        }

        if (User.IsInRole("Patient"))
        {
            var patientId = await _context.Patients
                .AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => (long?)item.Id)
                .FirstOrDefaultAsync();

            return new BookingScopeResult
            {
                IsValid = patientId.HasValue,
                FixedPatientId = patientId
            };
        }

        if (User.IsInRole("Doctor"))
        {
            var doctorId = await _context.Doctors
                .AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => (long?)item.Id)
                .FirstOrDefaultAsync();

            if (!doctorId.HasValue)
            {
                return new BookingScopeResult();
            }

            var patientIds = await _context.Appointments
                .AsNoTracking()
                .Where(item => item.DoctorId == doctorId.Value)
                .Select(item => item.PatientId)
                .Distinct()
                .ToListAsync();

            return new BookingScopeResult
            {
                IsValid = true,
                FixedDoctorId = doctorId,
                AllowedPatientIds = patientIds
            };
        }

        return new BookingScopeResult();
    }

    private static DateTime GetSuggestedBookingDate()
    {
        var now = DateTime.Now;
        return now.Hour >= 17 ? now.Date.AddDays(1) : now.Date;
    }

    private sealed class BookingScopeResult
    {
        public bool IsValid { get; set; }

        public long? FixedPatientId { get; set; }

        public long? FixedDoctorId { get; set; }

        public IReadOnlyList<long>? AllowedPatientIds { get; set; }
    }

    private sealed class AvailabilitySlotResult
    {
        public string Time { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string StatusLabel { get; set; } = string.Empty;
    }
}
