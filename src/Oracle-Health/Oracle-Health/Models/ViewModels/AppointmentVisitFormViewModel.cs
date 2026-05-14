using System.ComponentModel.DataAnnotations;

namespace Oracle_Health.Models.ViewModels;

public class AppointmentVisitFormViewModel
{
    public long AppointmentId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public DateTime AppointmentDate { get; set; }

    [Required]
    [Display(Name = "Diagnosis")]
    public string Diagnosis { get; set; } = string.Empty;

    [Display(Name = "Prescription")]
    public string? Prescription { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

