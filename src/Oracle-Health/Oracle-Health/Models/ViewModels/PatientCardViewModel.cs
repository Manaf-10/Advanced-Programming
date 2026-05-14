namespace Oracle_Health.Models.ViewModels;

public class PatientCardViewModel
{
    public long Id { get; set; }

    public long PatientReference { get; set; }

    public string FullName { get; set; } = string.Empty;

    public long Cpr { get; set; }
}

