using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle_Health.Models;
using Oracle_Health.Models.ViewModels;

namespace Oracle_Health.Controllers;

[Route("specializations")]
public class SpecializationsController : Controller
{
    private readonly ClinicManagementSystemContext _context;

    public SpecializationsController(ClinicManagementSystemContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Clinic Manager,Receptionist")]
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var model = await BuildIndexViewModel();
        return View("~/Views/Specializations/Index.cshtml", model);
    }

    [Authorize(Roles = "Clinic Manager")]
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SpecializationEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Enter a valid specialization name.";
            return RedirectToAction(nameof(Index));
        }

        var normalizedName = model.Name.Trim();
        var exists = await _context.Specializations
            .AnyAsync(item => item.Name.ToLower() == normalizedName.ToLower());

        if (exists)
        {
            TempData["ErrorMessage"] = "That specialization already exists.";
            return RedirectToAction(nameof(Index));
        }

        _context.Specializations.Add(new Specialization { Name = normalizedName });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Specialization created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Clinic Manager")]
    [HttpPut("{id:long}")]
    [HttpPost("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(long id, SpecializationEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Enter a valid specialization name.";
            return RedirectToAction(nameof(Index));
        }

        var specialization = await _context.Specializations.FirstOrDefaultAsync(item => item.Id == id);
        if (specialization is null)
        {
            return NotFound();
        }

        var normalizedName = model.Name.Trim();
        var exists = await _context.Specializations
            .AnyAsync(item => item.Id != id && item.Name.ToLower() == normalizedName.ToLower());

        if (exists)
        {
            TempData["ErrorMessage"] = "Another specialization already uses that name.";
            return RedirectToAction(nameof(Index));
        }

        specialization.Name = normalizedName;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Specialization updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Clinic Manager")]
    [HttpPost("/doctors/{id:long}/specializations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignToDoctor(long id, DoctorSpecializationAssignViewModel model)
    {
        if (id != model.DoctorId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Select a doctor and specialization.";
            return RedirectToAction(nameof(Index));
        }

        var doctor = await _context.Doctors
            .Include(item => item.Specializations)
            .FirstOrDefaultAsync(item => item.Id == id);

        var specialization = await _context.Specializations
            .FirstOrDefaultAsync(item => item.Id == model.SpecializationId);

        if (doctor is null || specialization is null)
        {
            return NotFound();
        }

        if (!doctor.Specializations.Any(item => item.Id == specialization.Id))
        {
            doctor.Specializations.Add(specialization);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Specialization assigned to doctor.";
        }
        else
        {
            TempData["ErrorMessage"] = "That doctor already has this specialization.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<SpecializationIndexViewModel> BuildIndexViewModel()
    {
        var specializations = await _context.Specializations
            .AsNoTracking()
            .Include(item => item.Doctors)
                .ThenInclude(doctor => doctor.User)
            .OrderBy(item => item.Name)
            .ToListAsync();

        var doctors = await _context.Doctors
            .AsNoTracking()
            .Include(item => item.User)
            .OrderBy(item => item.User.FirstName)
            .ThenBy(item => item.User.LastName)
            .Select(item => new ManagerSelectOptionViewModel
            {
                Id = item.Id,
                Label = "Dr. " + item.User.FirstName + " " + item.User.LastName
            })
            .ToListAsync();

        return new SpecializationIndexViewModel
        {
            Specializations = specializations
                .Select(item => new SpecializationListItemViewModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Doctors = item.Doctors
                        .OrderBy(doctor => doctor.User.FirstName)
                        .ThenBy(doctor => doctor.User.LastName)
                        .Select(doctor => "Dr. " + doctor.User.FirstName + " " + doctor.User.LastName)
                        .ToList()
                })
                .ToList(),
            Doctors = doctors,
            SpecializationOptions = specializations
                .Select(item => new ManagerSelectOptionViewModel
                {
                    Id = item.Id,
                    Label = item.Name
                })
                .ToList()
        };
    }
}

