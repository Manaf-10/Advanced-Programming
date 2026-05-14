namespace Oracle_Health.Models.ViewModels;

public class DoctorIndexViewModel
{
    public IReadOnlyList<DoctorListItemViewModel> Doctors { get; set; } = [];
}

public class DoctorListItemViewModel
{
    public long Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public IReadOnlyList<string> Specializations { get; set; } = [];

    public int UpcomingAppointments { get; set; }

    public IReadOnlyList<DoctorScheduleItemViewModel> Schedule { get; set; } = [];
}

public class DoctorScheduleItemViewModel
{
    public string DayOfWeek { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsOnLeave { get; set; }
}

