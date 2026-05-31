using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Api.Dtos;
using Oracle_Health.Models;

namespace Oracle_Health.Api.Controllers;

[ApiController]
[Authorize(Roles = "Clinic Manager")]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ClinicManagementSystemContext _context;

    public ReportsController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ClinicSummaryReport>> Summary()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var totalAppointments = await _context.Appointments.CountAsync();
        var upcomingAppointments = await _context.Appointments.CountAsync(item => item.Date >= today);
        var completedThisMonth = await _context.Appointments.CountAsync(item =>
            item.Status == AppointmentStatus.Completed && item.Date >= monthStart);
        var activeDoctors = await _context.Doctors.CountAsync();
        var registeredPatients = await _context.Patients.CountAsync();

        return new ClinicSummaryReport(
            totalAppointments,
            upcomingAppointments,
            completedThisMonth,
            activeDoctors,
            registeredPatients);
    }

    [HttpGet("doctor-workload")]
    public async Task<ActionResult<IReadOnlyList<DoctorWorkloadReportItem>>> DoctorWorkload()
    {
        var items = await _context.Doctors
            .AsNoTracking()
            .Include(doctor => doctor.User)
            .Include(doctor => doctor.Appointments)
            .OrderBy(doctor => doctor.User.FirstName)
            .ThenBy(doctor => doctor.User.LastName)
            .Select(doctor => new DoctorWorkloadReportItem(
                doctor.Id,
                doctor.User.FirstName + " " + doctor.User.LastName,
                doctor.Appointments.Count,
                doctor.Appointments.Count(appointment => appointment.Status == AppointmentStatus.Completed),
                doctor.Appointments.Count(appointment =>
                    appointment.Status == AppointmentStatus.Cancelled ||
                    appointment.Status == AppointmentStatus.Missed)))
            .ToListAsync();

        return items;
    }

    [HttpGet("cancellations")]
    public async Task<ActionResult<CancellationReport>> Cancellations()
    {
        var totalAppointments = await _context.Appointments.CountAsync();
        var cancelled = await _context.Appointments.CountAsync(item => item.Status == AppointmentStatus.Cancelled);
        var missed = await _context.Appointments.CountAsync(item => item.Status == AppointmentStatus.Missed);
        var affected = cancelled + missed;
        var rate = totalAppointments == 0 ? 0 : Math.Round((decimal)affected / totalAppointments * 100, 2);

        return new CancellationReport(totalAppointments, cancelled, missed, rate);
    }

    [HttpGet("appointment-status")]
    public async Task<ActionResult<IReadOnlyList<AppointmentStatusReportItem>>> AppointmentStatusBreakdown()
    {
        var grouped = await _context.Appointments
            .AsNoTracking()
            .GroupBy(appointment => appointment.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .OrderBy(item => item.Status)
            .ToListAsync();

        return grouped
            .Select(item => new AppointmentStatusReportItem(
                AppointmentStatus.ToDisplayName(item.Status),
                item.Count))
            .ToList();
    }
}
