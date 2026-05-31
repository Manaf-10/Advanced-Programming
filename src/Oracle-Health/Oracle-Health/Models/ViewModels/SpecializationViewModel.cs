using System.ComponentModel.DataAnnotations;

namespace Oracle_Health.Models.ViewModels;

public class SpecializationIndexViewModel
{
    public SpecializationEditViewModel NewSpecialization { get; set; } = new();

    public DoctorSpecializationAssignViewModel Assignment { get; set; } = new();

    public IReadOnlyList<SpecializationListItemViewModel> Specializations { get; set; } = [];

    public IReadOnlyList<ManagerSelectOptionViewModel> Doctors { get; set; } = [];

    public IReadOnlyList<ManagerSelectOptionViewModel> SpecializationOptions { get; set; } = [];
}

public class SpecializationListItemViewModel
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<string> Doctors { get; set; } = [];
}

public class SpecializationEditViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Specialization name is required")]
    [StringLength(100)]
    [Display(Name = "Specialization name")]
    public string Name { get; set; } = string.Empty;
}

public class DoctorSpecializationAssignViewModel
{
    [Required(ErrorMessage = "Doctor is required")]
    [Display(Name = "Doctor")]
    public long DoctorId { get; set; }

    [Required(ErrorMessage = "Specialization is required")]
    [Display(Name = "Specialization")]
    public long SpecializationId { get; set; }
}

