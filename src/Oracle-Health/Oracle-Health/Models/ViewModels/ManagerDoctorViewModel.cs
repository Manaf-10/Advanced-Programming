using System.ComponentModel.DataAnnotations;

namespace Oracle_Health.Models.ViewModels;

public class ManagerDoctorDetailsViewModel
{
    public long DoctorId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DoctorProfileEditViewModel Profile { get; set; } = new();

    public DoctorAvailabilityEditViewModel NewAvailability { get; set; } = new();

    public IReadOnlyList<DoctorAvailabilityEditViewModel> Availability { get; set; } = [];

    public IReadOnlyList<ManagerAppointmentEditViewModel> Appointments { get; set; } = [];

    public IReadOnlyList<ManagerSelectOptionViewModel> Doctors { get; set; } = [];

    public IReadOnlyList<ManagerSelectOptionViewModel> Patients { get; set; } = [];

    public IReadOnlyList<ManagerSelectOptionViewModel> Statuses { get; set; } = [];
}

public class DoctorProfileEditViewModel
{
    public long DoctorId { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
}

public class DoctorAvailabilityEditViewModel
{
    public long? ScheduleId { get; set; }

    public long DoctorId { get; set; }

    [Required(ErrorMessage = "Date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Start time is required")]
    [DataType(DataType.Time)]
    [Display(Name = "Start time")]
    public TimeSpan? StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    [DataType(DataType.Time)]
    [Display(Name = "End time")]
    public TimeSpan? EndTime { get; set; }

    [Display(Name = "Mark as leave")]
    public bool IsOnLeave { get; set; }
}

public class ManagerAppointmentEditViewModel
{
    public long AppointmentId { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public long PatientId { get; set; }

    [Required]
    [Display(Name = "Doctor")]
    public long DoctorId { get; set; }

    [Required(ErrorMessage = "Date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime? AppointmentDate { get; set; }

    [Required(ErrorMessage = "Time is required")]
    [DataType(DataType.Time)]
    [Display(Name = "Time")]
    public TimeSpan? AppointmentTime { get; set; }

    [Range(15, 120)]
    [Display(Name = "Duration")]
    public int DurationMinutes { get; set; }

    [Display(Name = "Status")]
    public int Status { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;
}

public class ManagerSelectOptionViewModel
{
    public long Id { get; set; }

    public string Label { get; set; } = string.Empty;
}

