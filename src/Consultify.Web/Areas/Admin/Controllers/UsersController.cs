using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Areas.Admin.ViewModels;

namespace Consultify.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IAdminService _adminService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UsersController(IAdminService adminService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _adminService = adminService;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index(string? role, string? search, int page = 1)
    {
        var pageSize = 20;
        var users = await _adminService.GetUsersAsync(role, search, page, pageSize);
        var total = _context.Users.Count();

        var vm = new UserListVM
        {
            Users = users,
            RoleFilter = role,
            SearchTerm = search,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var user = await _adminService.GetUserByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var appointmentCount = await _context.Appointments
            .CountAsync(a => a.CustomerUserId == id);

        var vm = new UserDetailVM
        {
            User = user,
            Role = roles.FirstOrDefault() ?? "None",
            AppointmentCount = appointmentCount
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _adminService.ToggleUserStatusAsync(id);
        if (result)
        {
            TempData["Success"] = "User status updated.";
        }
        else
        {
            TempData["Error"] = "User not found.";
        }
        return RedirectToAction(nameof(Index));
    }
}
