using System.Diagnostics;
using System.Security.Claims;
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
                    Status = AppointmentStatus.ToDisplayName(appointment.Status),
                    StatusActions = GetVisibleStatusActions(appointment)
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

        private List<AppointmentStatusActionViewModel> GetVisibleStatusActions(Appointment appointment)
        {
            return AppointmentStatus.GetAllowedNextStatuses(appointment.Status)
                .Where(nextStatus => CanCurrentUserSetStatus(appointment, nextStatus))
                .Select(nextStatus => new AppointmentStatusActionViewModel
                {
                    Status = nextStatus,
                    Label = AppointmentStatus.ToActionLabel(nextStatus)
                })
                .ToList();
        }

        private bool CanCurrentUserSetStatus(Appointment appointment, int nextStatus)
        {
            if (User.IsInRole("Admin"))
            {
                return nextStatus is AppointmentStatus.Confirmed
                    or AppointmentStatus.Cancelled
                    or AppointmentStatus.Missed;
            }

            if (User.IsInRole("Reception"))
            {
                return nextStatus is AppointmentStatus.Confirmed
                    or AppointmentStatus.CheckedIn
                    or AppointmentStatus.Cancelled
                    or AppointmentStatus.Missed;
            }

            if (User.IsInRole("Doctor") && nextStatus == AppointmentStatus.InProgress)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return long.TryParse(userIdClaim, out var userId) && appointment.Doctor.UserId == userId;
            }

            return false;
        }
    }
}
