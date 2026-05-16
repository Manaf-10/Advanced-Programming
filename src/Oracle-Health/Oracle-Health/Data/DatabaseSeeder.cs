using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Services;

namespace Oracle_Health.Data;

public static class DatabaseSeeder
{
    private const string SeedPassword = "Password123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicManagementSystemContext>();

        await context.Database.EnsureCreatedAsync();

        if (await context.Users.AnyAsync(user => user.Email == "manager@oraclehealth.test"))
        {
            return;
        }

        var cardiology = new Specialization { Name = "Cardiology" };
        var pediatrics = new Specialization { Name = "Pediatrics" };
        var dermatology = new Specialization { Name = "Dermatology" };
        var familyMedicine = new Specialization { Name = "Family Medicine" };

        context.Specializations.AddRange(cardiology, pediatrics, dermatology, familyMedicine);

        var managerUser = CreateUser("Mariam", "Al Haddad", "manager@oraclehealth.test", UserRole.Admin);
        var receptionistUser = CreateUser("Noor", "Khalil", "reception@oraclehealth.test", UserRole.Reception);
        var doctorOneUser = CreateUser("Ahmed", "Naser", "ahmed.naser@oraclehealth.test", UserRole.Doctor);
        var doctorTwoUser = CreateUser("Sara", "Mansoor", "sara.mansoor@oraclehealth.test", UserRole.Doctor);
        var patientOneUser = CreateUser("Ali", "Hassan", "ali.hassan@oraclehealth.test", UserRole.Patient);
        var patientTwoUser = CreateUser("Fatima", "Saleh", "fatima.saleh@oraclehealth.test", UserRole.Patient);
        var patientThreeUser = CreateUser("Omar", "Yousif", "omar.yousif@oraclehealth.test", UserRole.Patient);

        context.Users.AddRange(
            managerUser,
            receptionistUser,
            doctorOneUser,
            doctorTwoUser,
            patientOneUser,
            patientTwoUser,
            patientThreeUser);

        var doctorOne = new Doctor { User = doctorOneUser };
        doctorOne.Specializations.Add(cardiology);
        doctorOne.Specializations.Add(familyMedicine);

        var doctorTwo = new Doctor { User = doctorTwoUser };
        doctorTwo.Specializations.Add(pediatrics);
        doctorTwo.Specializations.Add(dermatology);

        var patientOne = new Patient { User = patientOneUser, PatientId = 10001, Cpr = 900101123 };
        var patientTwo = new Patient { User = patientTwoUser, PatientId = 10002, Cpr = 910202456 };
        var patientThree = new Patient { User = patientThreeUser, PatientId = 10003, Cpr = 920303789 };

        context.Doctors.AddRange(doctorOne, doctorTwo);
        context.Patients.AddRange(patientOne, patientTwo, patientThree);

        var today = DateTime.Today;

        var completedAppointment = new Appointment
        {
            Patient = patientOne,
            Doctor = doctorOne,
            Date = today.AddDays(-7).AddHours(10),
            DurationMinutes = 30,
            Status = AppointmentStatus.Completed
        };

        var confirmedAppointment = new Appointment
        {
            Patient = patientTwo,
            Doctor = doctorOne,
            Date = today.AddDays(1).AddHours(9),
            DurationMinutes = 30,
            Status = AppointmentStatus.Confirmed
        };

        var requestedAppointment = new Appointment
        {
            Patient = patientThree,
            Doctor = doctorTwo,
            Date = today.AddDays(2).AddHours(13),
            DurationMinutes = 45,
            Status = AppointmentStatus.Requested
        };

        var checkedInAppointment = new Appointment
        {
            Patient = patientOne,
            Doctor = doctorTwo,
            Date = today.AddHours(11),
            DurationMinutes = 30,
            Status = AppointmentStatus.CheckedIn
        };

        context.Appointments.AddRange(
            completedAppointment,
            confirmedAppointment,
            requestedAppointment,
            checkedInAppointment);

        context.Schedules.AddRange(
            CreateSchedule(doctorOne, "Sunday", today.AddDays(1).AddHours(8), today.AddDays(1).AddHours(14), confirmedAppointment),
            CreateSchedule(doctorOne, "Monday", today.AddDays(2).AddHours(8), today.AddDays(2).AddHours(14)),
            CreateSchedule(doctorTwo, "Sunday", today.AddHours(10), today.AddHours(16), checkedInAppointment),
            CreateSchedule(doctorTwo, "Tuesday", today.AddDays(2).AddHours(12), today.AddDays(2).AddHours(18), requestedAppointment),
            CreateSchedule(doctorTwo, "Thursday", today.AddDays(4).AddHours(9), today.AddDays(4).AddHours(12), isOnLeave: true));

        context.Visits.Add(new Visit
        {
            Patient = patientOne,
            Doctor = doctorOne,
            Appointment = completedAppointment,
            Notes = "Diagnosis: Mild hypertension\nNotes: Patient advised to reduce sodium intake and monitor blood pressure.",
            Prescription = "Amlodipine 5mg once daily for 30 days",
            CreatedAt = today.AddDays(-7).AddHours(10.5)
        });

        context.Notifications.AddRange(
            new Notification
            {
                User = patientTwoUser,
                Message = "Your appointment with Dr. Ahmed Naser is confirmed for tomorrow at 9:00 AM.",
                CreatedAt = DateTime.Now.AddHours(-3)
            },
            new Notification
            {
                User = doctorTwoUser,
                Message = "Ali Hassan has checked in for today's appointment.",
                CreatedAt = DateTime.Now.AddMinutes(-20)
            },
            new Notification
            {
                User = receptionistUser,
                Message = "New requested appointment from Omar Yousif needs confirmation.",
                CreatedAt = DateTime.Now.AddMinutes(-10)
            });

        await context.SaveChangesAsync();
    }

    private static User CreateUser(string firstName, string lastName, string email, int role)
    {
        return new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Password = PasswordService.Hash(SeedPassword),
            Role = role
        };
    }

    private static Schedule CreateSchedule(
        Doctor doctor,
        string dayOfWeek,
        DateTime startTime,
        DateTime endTime,
        Appointment? appointment = null,
        bool isOnLeave = false)
    {
        return new Schedule
        {
            Doctor = doctor,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            Appointment = appointment,
            IsOnLeave = isOnLeave
        };
    }
}
