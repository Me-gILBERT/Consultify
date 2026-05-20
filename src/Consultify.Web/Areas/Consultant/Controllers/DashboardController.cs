using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Areas.Consultant.ViewModels;

namespace Consultify.Web.Areas.Consultant.Controllers;

[Area("Consultant")]
[Authorize(Roles = "Consultant")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(IDashboardService dashboardService, UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var stats = await _dashboardService.GetConsultantStatsAsync(Guid.Parse(userId));

        var vm = new ConsultantDashboardVM
        {
            UpcomingAppointments = stats.UpcomingAppointments,
            TodayCount = stats.TodayCount,
            WeeklyCount = stats.WeeklyCount,
            TotalAppointments = stats.TotalAppointments,
            AverageRating = stats.AverageRating
        };

        return View(vm);
    }
}
