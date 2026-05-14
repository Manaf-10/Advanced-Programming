using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health.Controllers;

public class DoctorController : Controller
{
    private readonly ClinicManagementSystemContext _context;

    public DoctorController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var doctors = await _context.Doctors
            .AsNoTracking()
            .Include(doctor => doctor.User)
            .Include(doctor => doctor.Specializations)
            .Include(doctor => doctor.Schedules)
            .Include(doctor => doctor.Appointments)
            .OrderBy(doctor => doctor.User.FirstName)
            .ThenBy(doctor => doctor.User.LastName)
            .Select(doctor => new DoctorListItemViewModel
            {
                Id = doctor.Id,
                FullName = doctor.User.FirstName + " " + doctor.User.LastName,
                Email = doctor.User.Email,
                Specializations = doctor.Specializations
                    .OrderBy(specialization => specialization.Name)
                    .Select(specialization => specialization.Name)
                    .ToList(),
                UpcomingAppointments = doctor.Appointments.Count(appointment => appointment.Date >= today),
                Schedule = doctor.Schedules
                    .OrderBy(schedule => schedule.DayOfWeek)
                    .ThenBy(schedule => schedule.StartTime)
                    .Select(schedule => new DoctorScheduleItemViewModel
                    {
                        DayOfWeek = schedule.DayOfWeek,
                        StartTime = schedule.StartTime,
                        EndTime = schedule.EndTime,
                        IsOnLeave = schedule.IsOnLeave == true
                    })
                    .ToList()
            })
            .ToListAsync();

        return View(new DoctorIndexViewModel { Doctors = doctors });
    }
}

