namespace Oracle_Health.Api.Dtos;

public record AppointmentUpdateRequest(
    long PatientId,
    long DoctorId,
    DateTime Date,
    int DurationMinutes,
    int Status);

public record ScheduleRequest(DateTime StartTime, DateTime EndTime, bool IsOnLeave);

public record VisitDto(
    long Id,
    DateTime AppointmentDate,
    string DoctorName,
    string Notes,
    string? Prescription);

public record PrescriptionDto(
    long Id,
    long PatientId,
    string PatientName,
    string DoctorName,
    DateTime AppointmentDate,
    string? Medicine,
    string Notes);

public record StaffUserDto(long Id, string FirstName, string LastName, string Email, string Role);

