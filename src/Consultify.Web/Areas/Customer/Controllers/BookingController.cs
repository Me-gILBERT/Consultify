using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Areas.Customer.ViewModels;

namespace Consultify.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IConsultantService _consultantService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookingController(IBookingService bookingService, IConsultantService consultantService, UserManager<ApplicationUser> userManager)
    {
        _bookingService = bookingService;
        _consultantService = consultantService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Confirm(int slotId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var profile = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));
        var consultant = await _consultantService.GetProfileByUserIdAsync(Guid.Parse(userId));

        var slot = await _bookingService.GetAvailableSlotsAsync(0);
        var allSlots = await Task.Run(() => new List<TimeSlot>());

        var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var timeSlot = await context.TimeSlots
            .Include(ts => ts.ConsultantProfile).ThenInclude(cp => cp.User)
            .FirstOrDefaultAsync(ts => ts.Id == slotId);

        if (timeSlot == null || timeSlot.IsBooked)
        {
            TempData["Error"] = "This slot is no longer available.";
            return RedirectToAction("Index", "Consultants");
        }

        var vm = new BookConfirmVM
        {
            TimeSlotId = timeSlot.Id,
            ConsultantName = timeSlot.ConsultantProfile.User.FullName,
            StartTime = timeSlot.StartTime,
            EndTime = timeSlot.EndTime
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(BookConfirmVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        try
        {
            var appointment = await _bookingService.BookSlotAsync(vm.TimeSlotId, Guid.Parse(userId), vm.Notes);
            TempData["Success"] = "Appointment booked successfully!";
            return RedirectToAction("Index", "Appointments");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }
}
