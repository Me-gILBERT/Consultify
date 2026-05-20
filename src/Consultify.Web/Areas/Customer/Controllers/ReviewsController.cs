using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;

namespace Consultify.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsController(IReviewService reviewService, UserManager<ApplicationUser> userManager)
    {
        _reviewService = reviewService;
        _userManager = userManager;
    }

    public IActionResult Create(int appointmentId)
    {
        ViewBag.AppointmentId = appointmentId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int appointmentId, int rating, string? comment)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        try
        {
            await _reviewService.SubmitReviewAsync(appointmentId, Guid.Parse(userId), rating, comment);
            TempData["Success"] = "Review submitted!";
            return RedirectToAction("Index", "Appointments");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.AppointmentId = appointmentId;
            return View();
        }
    }
}
