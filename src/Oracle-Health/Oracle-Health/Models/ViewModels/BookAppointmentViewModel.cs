namespace Oracle_Health.Models.ViewModels;

public class BookAppointmentViewModel
{
    public List<SpecializationOptionViewModel> Specializations { get; set; } = new();
    public List<DoctorOptionViewModel> Doctors { get; set; } = new();
}

public class SpecializationOptionViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DoctorOptionViewModel
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public long SpecializationId { get; set; }
}

// Submitted by the patient when booking
public class BookAppointmentFormViewModel
{
    public long DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
}
