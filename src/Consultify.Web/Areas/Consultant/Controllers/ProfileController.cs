using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Areas.Consultant.ViewModels;

namespace Consultify.Web.Areas.Consultant.Controllers;

[Area("Consultant")]
[Authorize(Roles = "Consultant")]
public class ProfileController : Controller
{
    private readonly IConsultantService _consultantService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(IConsultantService consultantService, UserManager<ApplicationUser> userManager)
    {
        _consultantService = consultantService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        if (profile == null) return NotFound("Consultant profile not found.");

        return View(profile);
    }

    public async Task<IActionResult> Edit()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        if (profile == null) return NotFound();

        var vm = new ConsultantProfileVM
        {
            Bio = profile.Bio,
            Specialization = profile.Specialization,
            HourlyRate = profile.HourlyRate,
            YearsOfExperience = profile.YearsOfExperience
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ConsultantProfileVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        if (profile == null) return NotFound();

        profile.Bio = vm.Bio;
        profile.Specialization = vm.Specialization;
        profile.HourlyRate = vm.HourlyRate;
        profile.YearsOfExperience = vm.YearsOfExperience;

        await _consultantService.UpdateProfileAsync(profile);
        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
