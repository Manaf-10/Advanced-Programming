namespace Oracle_Health.Models.ViewModels;

public class AppointmentListItemViewModel
{
    public long Id { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<AppointmentStatusActionViewModel> StatusActions { get; set; } = new();
}

public class AppointmentStatusActionViewModel
{
    public int Status { get; set; }

    public string Label { get; set; } = string.Empty;
}
