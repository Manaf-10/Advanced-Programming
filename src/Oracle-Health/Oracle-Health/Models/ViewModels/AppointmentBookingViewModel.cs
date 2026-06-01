using System.ComponentModel.DataAnnotations;

namespace Oracle_Health.Models.ViewModels;

public class AppointmentBookingViewModel
{
    [Display(Name = "Patient")]
    public long? SelectedPatientId { get; set; }

    [Display(Name = "Specialization")]
    public long? SelectedSpecializationId { get; set; }

    [Display(Name = "Doctor")]
    public long? SelectedDoctorId { get; set; }

    [Display(Name = "Date")]
    [DataType(DataType.Date)]
    public DateTime? AppointmentDate { get; set; }

    [Display(Name = "Time")]
    [DataType(DataType.Time)]
    public TimeSpan? AppointmentTime { get; set; }

    [Display(Name = "Duration (minutes)")]
    [Range(15, 120)]
    public int DurationMinutes { get; set; } = 30;

    public bool CanSelectPatient { get; set; }

    public bool CanSelectDoctor { get; set; } = true;

    public List<AppointmentBookingSelectOptionViewModel> Specializations { get; set; } = new();

    public List<AppointmentBookingDoctorOptionViewModel> Doctors { get; set; } = new();

    public List<AppointmentBookingPatientOptionViewModel> Patients { get; set; } = new();
}

public class AppointmentBookingSelectOptionViewModel
{
    public long Id { get; set; }

    public string Label { get; set; } = string.Empty;
}

public class AppointmentBookingDoctorOptionViewModel
{
    public long Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string SpecializationSummary { get; set; } = string.Empty;

    public List<long> SpecializationIds { get; set; } = new();
}

public class AppointmentBookingPatientOptionViewModel
{
    public long Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public long PatientReference { get; set; }

    public long Cpr { get; set; }
}
