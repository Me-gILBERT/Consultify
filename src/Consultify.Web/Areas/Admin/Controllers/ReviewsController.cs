using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Areas.Admin.ViewModels;

namespace Consultify.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewsController : Controller
{
    private readonly IAdminService _adminService;

    public ReviewsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var pageSize = 20;
        var reviews = await _adminService.GetAllReviewsAsync(page, pageSize);

        var vm = new ReviewListVM
        {
            Reviews = reviews,
            Page = page,
            TotalPages = 1
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _adminService.DeleteReviewAsync(id);
        if (result)
        {
            TempData["Success"] = "Review deleted.";
        }
        else
        {
            TempData["Error"] = "Review not found.";
        }
        return RedirectToAction(nameof(Index));
    }
}
