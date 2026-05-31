namespace Oracle_Health.Models;

public static class UserRole
{
    public const int Patient = 0;
    public const int Doctor = 1;
    public const int Receptionist = 2;
    public const int ClinicManager = 3;

    public static string ToClaimValue(int role)
    {
        return role switch
        {
            Doctor => "Doctor",
            Receptionist => "Receptionist",
            ClinicManager => "Clinic Manager",
            _ => "Patient"
        };
    }

    public static string ToDisplayName(int role)
    {
        return role switch
        {
            Doctor => "Doctor",
            Receptionist => "Receptionist",
            ClinicManager => "Clinic Manager",
            _ => "Patient"
        };
    }
}
