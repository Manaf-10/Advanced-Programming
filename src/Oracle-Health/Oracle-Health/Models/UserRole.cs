namespace Oracle_Health.Models;

public static class UserRole
{
    public const int Patient = 0;
    public const int Doctor = 1;
    public const int Reception = 2;
    public const int Admin = 3;

    public static string ToClaimValue(int role)
    {
        return role switch
        {
            Doctor => "Doctor",
            Reception => "Reception",
            Admin => "Admin",
            _ => "Patient"
        };
    }

    public static string ToDisplayName(int role)
    {
        return role switch
        {
            Doctor => "Doctor",
            Reception => "Receptionist",
            Admin => "Clinic Manager",
            _ => "Patient"
        };
    }
}
