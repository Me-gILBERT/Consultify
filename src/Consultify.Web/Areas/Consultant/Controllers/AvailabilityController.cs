using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Areas.Consultant.ViewModels;

namespace Consultify.Web.Areas.Consultant.Controllers;

[Area("Consultant")]
[Authorize(Roles = "Consultant")]
public class AvailabilityController : Controller
{
    private readonly IConsultantService _consultantService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AvailabilityController(IConsultantService consultantService, UserManager<ApplicationUser> userManager)
    {
        _consultantService = consultantService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        if (profile == null) return NotFound("Consultant profile not found. Please contact support.");

        var slots = await _consultantService.GetTimeSlotsAsync(profile.Id);
        var grouped = slots
            .GroupBy(s => s.StartTime.Date)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartTime).ToList());

        return View(grouped);
    }

    public IActionResult Create()
    {
        return View(new CreateTimeSlotVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTimeSlotVM model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        if (profile == null) return NotFound("Consultant profile not found.");

        try
        {
            var date = DateTime.SpecifyKind(model.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var slots = await _consultantService.CreateTimeSlotsAsync(profile.Id, date, model.StartTime, model.EndTime);
            TempData["Success"] = $"{slots.Count} time slot(s) created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        if (profile == null) return NotFound();

        var result = await _consultantService.RemoveTimeSlotAsync(id, profile.Id);
        if (!result)
        {
            TempData["Error"] = "Could not delete slot. It may be booked or already removed.";
        }
        else
        {
            TempData["Success"] = "Time slot deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}
