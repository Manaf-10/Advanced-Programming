using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;

namespace Oracle_Health.Services;

public class ValidationService : IValidationService
{
    private readonly ClinicManagementSystemContext _context;

    public ValidationService(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    public async Task<(bool IsValid, string Message)> CheckAppointmentConflict(
        long doctorId,
        DateTime date,
        int duration,
        long? excludeAppointmentId = null)
    {
        var end = date.AddMinutes(duration);

        var isWorking = await _context.Schedules.AnyAsync(schedule =>
            schedule.DoctorId == doctorId
            && schedule.IsOnLeave != true
            && schedule.StartTime.Date == date.Date
            && schedule.StartTime <= date
            && schedule.EndTime >= end);

        if (!isWorking)
        {
            return (false, "Doctor is not scheduled to work at this time.");
        }

        var hasLeaveConflict = await _context.Schedules.AnyAsync(schedule =>
            schedule.DoctorId == doctorId
            && schedule.IsOnLeave == true
            && schedule.StartTime < end
            && schedule.EndTime > date);

        if (hasLeaveConflict)
        {
            return (false, "Doctor is on leave during this time.");
        }

        var hasAppointmentOverlap = await _context.Appointments.AnyAsync(appointment =>
            appointment.DoctorId == doctorId
            && appointment.Id != excludeAppointmentId
            && appointment.Status != AppointmentStatus.Cancelled
            && appointment.Status != AppointmentStatus.Missed
            && date < appointment.Date.AddMinutes(appointment.DurationMinutes)
            && appointment.Date < end);

        if (hasAppointmentOverlap)
        {
            return (false, "This time slot overlaps with another appointment.");
        }

        return (true, string.Empty);
    }

    public Task<List<Appointment>> GetImpactedAppointments(long doctorId, DateTime start, DateTime end)
    {
        return _context.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Patient)
                .ThenInclude(patient => patient.User)
            .Where(appointment =>
                appointment.DoctorId == doctorId
                && appointment.Status != AppointmentStatus.Cancelled
                && appointment.Status != AppointmentStatus.Missed
                && appointment.Date < end
                && appointment.Date.AddMinutes(appointment.DurationMinutes) > start)
            .OrderBy(appointment => appointment.Date)
            .ToListAsync();
    }
}

