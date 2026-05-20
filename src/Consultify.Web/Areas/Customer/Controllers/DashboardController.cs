using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;

namespace Consultify.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var now = DateTime.UtcNow;
        var appointments = await _context.Appointments
            .Include(a => a.TimeSlot)
            .Include(a => a.ConsultantProfile).ThenInclude(cp => cp.User)
            .Where(a => a.CustomerUserId == Guid.Parse(userId))
            .OrderByDescending(a => a.TimeSlot.StartTime)
            .ToListAsync();

        ViewBag.Upcoming = appointments.Where(a => a.Status == AppointmentStatus.Scheduled && a.TimeSlot.StartTime > now).Take(5).ToList();
        ViewBag.Past = appointments.Where(a => a.Status != AppointmentStatus.Scheduled || a.TimeSlot.StartTime <= now).Take(5).ToList();

        return View();
    }
}
