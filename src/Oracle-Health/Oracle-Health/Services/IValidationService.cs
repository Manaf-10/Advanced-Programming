using Oracle_Health.Models;

namespace Oracle_Health.Services;

public interface IValidationService
{
    Task<(bool IsValid, string Message)> CheckAppointmentConflict(
        long doctorId,
        DateTime date,
        int duration,
        long? excludeAppointmentId = null);

    Task<List<Appointment>> GetImpactedAppointments(long doctorId, DateTime start, DateTime end);
}

