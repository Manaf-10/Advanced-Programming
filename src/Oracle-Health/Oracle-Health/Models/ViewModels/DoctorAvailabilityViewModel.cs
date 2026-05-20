namespace Oracle_Health.Models.ViewModels;

public class DoctorAvailabilityViewModel
{
    public DateTime WeekStart { get; set; }

    public DateTime PreviousWeekStart => WeekStart.AddDays(-7);

    public DateTime NextWeekStart => WeekStart.AddDays(7);

    public IReadOnlyList<DoctorAvailabilityDayViewModel> Days { get; set; } = [];
}

public class DoctorAvailabilityDayViewModel
{
    public DateTime Date { get; set; }

    public IReadOnlyList<DoctorAvailabilityBlockViewModel> Blocks { get; set; } = [];
}

public class DoctorAvailabilityBlockViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string Type { get; set; } = "working";
}
