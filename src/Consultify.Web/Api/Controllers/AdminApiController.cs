using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Api.ViewModels;

namespace Consultify.Web.Api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminApiController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminApiController(IAdminService adminService, UserManager<ApplicationUser> userManager)
    {
        _adminService = adminService;
        _userManager = userManager;
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<List<UserResponseDto>>>> GetUsers([FromQuery] string? role, [FromQuery] string? search)
    {
        var users = await _adminService.GetUsersAsync(role, search, 1, 100);

        var dtos = new List<UserResponseDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtos.Add(new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }

        return Ok(ApiResponse<List<UserResponseDto>>.Ok(dtos));
    }

    [HttpPut("users/{id}/toggle-status")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleStatus(Guid id)
    {
        var result = await _adminService.ToggleUserStatusAsync(id);
        if (!result)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        return Ok(ApiResponse<object>.Ok(new { }, "User status toggled."));
    }

    [HttpDelete("reviews/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteReview(int id)
    {
        var result = await _adminService.DeleteReviewAsync(id);
        if (!result)
            return NotFound(ApiResponse<object>.Fail("Review not found."));

        return Ok(ApiResponse<object>.Ok(new { }, "Review deleted."));
    }
}
