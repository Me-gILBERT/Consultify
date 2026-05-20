using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;

namespace Consultify.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBookingService _bookingService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AppointmentsController(ApplicationDbContext context, IBookingService bookingService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _bookingService = bookingService;
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
            .Include(a => a.Review)
            .Where(a => a.CustomerUserId == Guid.Parse(userId))
            .OrderByDescending(a => a.TimeSlot.StartTime)
            .ToListAsync();

        return View(appointments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        try
        {
            var appointment = await _bookingService.CancelAppointmentAsync(id, Guid.Parse(userId), reason);
            TempData["Success"] = "Appointment cancelled.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
