namespace Oracle_Health.Models.ViewModels;

public class ProfileViewModel
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public long? PatientReference { get; set; }

    public long? Cpr { get; set; }
}
