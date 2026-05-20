using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;

namespace Consultify.Web.Areas.Consultant.Controllers;

[Area("Consultant")]
[Authorize(Roles = "Consultant")]
public class AppointmentsController : Controller
{
    private readonly IConsultantService _consultantService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AppointmentsController(IConsultantService consultantService, UserManager<ApplicationUser> userManager)
    {
        _consultantService = consultantService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(AppointmentStatus? status)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        if (profile == null) return NotFound("Consultant profile not found.");

        var appointments = await _consultantService.GetAppointmentsAsync(profile.Id, status);

        ViewBag.StatusFilter = status;
        ViewBag.Statuses = new SelectList(
            Enum.GetValues<AppointmentStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }),
            "Value", "Text", status.HasValue ? (int)status.Value : null);

        return View(appointments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCompleted(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        if (profile == null) return NotFound();

        var result = await _consultantService.MarkCompletedAsync(id, profile.Id);
        if (!result)
        {
            TempData["Error"] = "Could not mark appointment as completed.";
        }
        else
        {
            TempData["Success"] = "Appointment marked as completed.";
        }

        return RedirectToAction(nameof(Index));
    }
}
