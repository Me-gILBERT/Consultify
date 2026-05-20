using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Api.ViewModels;

namespace Consultify.Web.Api.Controllers;

[Route("api/reviews")]
[ApiController]
public class ReviewsApiController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsApiController(IReviewService reviewService, UserManager<ApplicationUser> userManager)
    {
        _reviewService = reviewService;
        _userManager = userManager;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<object>>> Submit([FromBody] ReviewRequestDto request)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        try
        {
            var review = await _reviewService.SubmitReviewAsync(
                request.AppointmentId, Guid.Parse(userId), request.Rating, request.Comment);

            return Ok(ApiResponse<object>.Ok(new
            {
                review.Id,
                review.Rating,
                review.Comment,
                review.CreatedAt
            }, "Review submitted."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
