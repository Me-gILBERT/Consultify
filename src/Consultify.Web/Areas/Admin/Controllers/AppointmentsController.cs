using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Areas.Admin.ViewModels;

namespace Consultify.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AppointmentsController : Controller
{
    private readonly IAdminService _adminService;

    public AppointmentsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<IActionResult> Index(AppointmentStatus? status)
    {
        var appointments = await _adminService.GetAllAppointmentsAsync(status);

        ViewBag.StatusFilter = status;
        ViewBag.Statuses = new SelectList(
            Enum.GetValues<AppointmentStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }),
            "Value", "Text", status.HasValue ? (int)status.Value : null);

        var vm = new AppointmentListVM
        {
            Appointments = appointments,
            StatusFilter = status
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var result = await _adminService.CancelAppointmentAsync(id, reason ?? "Cancelled by admin");
        if (result)
        {
            TempData["Success"] = "Appointment cancelled.";
        }
        else
        {
            TempData["Error"] = "Appointment not found.";
        }
        return RedirectToAction(nameof(Index));
    }
}
