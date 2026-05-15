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

    public static IReadOnlyList<int> GetAllowedNextStatuses(int status)
    {
        return status switch
        {
            Requested => new[] { Confirmed, Cancelled },
            Confirmed => new[] { CheckedIn, Cancelled, Missed },
            CheckedIn => new[] { InProgress, Cancelled },
            InProgress => new[] { Completed, Cancelled },
            _ => Array.Empty<int>()
        };
    }

    public static bool CanTransition(int currentStatus, int nextStatus)
    {
        return GetAllowedNextStatuses(currentStatus).Contains(nextStatus);
    }

    public static string ToActionLabel(int status)
    {
        return status switch
        {
            Confirmed => "Confirm",
            CheckedIn => "Check in",
            InProgress => "Start appointment",
            Completed => "Complete",
            Cancelled => "Cancel",
            Missed => "Mark missed",
            _ => ToDisplayName(status)
        };
    }

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
