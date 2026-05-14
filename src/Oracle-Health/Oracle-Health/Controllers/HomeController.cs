using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ClinicManagementSystemContext _context;

        public HomeController(ILogger<HomeController> logger, ClinicManagementSystemContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var appointmentRecords = await _context.Appointments
                .AsNoTracking()
                .Include(appointment => appointment.Patient)
                    .ThenInclude(patient => patient.User)
                .Include(appointment => appointment.Doctor)
                    .ThenInclude(doctor => doctor.User)
                .OrderBy(appointment => appointment.Date)
                .ToListAsync();

            var appointments = appointmentRecords
                .Select(appointment => new AppointmentListItemViewModel
                {
                    Id = appointment.Id,
                    PatientName = appointment.Patient.User.FirstName + " " + appointment.Patient.User.LastName,
                    DoctorName = appointment.Doctor.User.FirstName + " " + appointment.Doctor.User.LastName,
                    Date = appointment.Date,
                    Status = AppointmentStatus.ToDisplayName(appointment.Status)
                })
                .ToList();

            return View(appointments);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
