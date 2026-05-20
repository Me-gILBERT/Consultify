using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Areas.Admin.ViewModels;

namespace Consultify.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _dashboardService.GetAdminStatsAsync();

        var vm = new DashboardStatsVM
        {
            TotalUsers = stats.TotalUsers,
            TotalConsultants = stats.TotalConsultants,
            TotalCustomers = stats.TotalCustomers,
            TotalAppointments = stats.TotalAppointments,
            AppointmentsByStatus = stats.AppointmentsByStatus,
            RecentAppointments = stats.RecentAppointments
        };

        return View(vm);
    }
}
