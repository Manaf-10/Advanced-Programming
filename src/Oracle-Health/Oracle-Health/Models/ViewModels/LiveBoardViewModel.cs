namespace Oracle_Health.Models.ViewModels;

public class LiveBoardViewModel
{
    public DateTime BoardDate { get; set; }

    public List<LiveBoardAppointmentItemViewModel> Items { get; set; } = new();

    public int WaitingCount => Items.Count(item => item.Status == "Confirmed" || item.Status == "Checked-In");

    public int InProgressCount => Items.Count(item => item.Status == "In-Progress");

    public int FinishedCount => Items.Count(item => item.Status == "Completed");
}

public class LiveBoardAppointmentItemViewModel
{
    public long AppointmentId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public List<string> Specializations { get; set; } = new();

    public DateTime Time { get; set; }

    public DateTime EndTime { get; set; }

    public string Status { get; set; } = string.Empty;
}
