namespace Oracle_Health.Models;

public static class AppointmentStatus
{
    public const int Requested = 0;
    public const int Confirmed = 1;
    public const int CheckedIn = 2;
    public const int InProgress = 3;
    public const int Completed = 4;
    public const int Cancelled = 5;
    public const int Missed = 6;

    public static string ToDisplayName(int status)
    {
        return status switch
        {
            Requested => "Requested",
            Confirmed => "Confirmed",
            CheckedIn => "Checked-In",
            InProgress => "In-Progress",
            Completed => "Completed",
            Cancelled => "Cancelled",
            Missed => "Missed",
            _ => "Unknown"
        };
    }
}

