using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Api.ViewModels;

namespace Consultify.Web.Api.Controllers;

[Route("api/consultants")]
[ApiController]
public class ConsultantsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IReviewService _reviewService;

    public ConsultantsApiController(ApplicationDbContext context, IReviewService reviewService)
    {
        _context = context;
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserResponseDto>>>> GetAll([FromQuery] string? search)
    {
        var query = _context.ConsultantProfiles
            .Include(cp => cp.User)
            .Where(cp => cp.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(cp =>
                cp.User.FirstName.ToLower().Contains(term) ||
                cp.User.LastName.ToLower().Contains(term) ||
                (cp.Specialization != null && cp.Specialization.ToLower().Contains(term)));
        }

        var consultants = await query
            .OrderBy(cp => cp.User.FirstName)
            .Select(cp => new UserResponseDto
            {
                Id = cp.UserId,
                Email = cp.User.Email!,
                FirstName = cp.User.FirstName,
                LastName = cp.User.LastName,
                IsActive = cp.IsActive,
                CreatedAt = cp.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<UserResponseDto>>.Ok(consultants));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(int id)
    {
        var profile = await _context.ConsultantProfiles
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.Id == id);

        if (profile == null)
            return NotFound(ApiResponse<object>.Fail("Consultant not found."));

        var rating = await _reviewService.GetAverageRatingAsync(id);
        var reviewCount = await _reviewService.GetReviewCountAsync(id);

        var result = new
        {
            profile.Id,
            profile.User.FirstName,
            profile.User.LastName,
            profile.User.Email,
            profile.Bio,
            profile.Specialization,
            profile.HourlyRate,
            profile.YearsOfExperience,
            AverageRating = rating,
            ReviewCount = reviewCount
        };

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{id}/reviews")]
    public async Task<ActionResult<ApiResponse<object>>> GetReviews(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var reviews = await _reviewService.GetReviewsForConsultantAsync(id, page, pageSize);

        var result = reviews.Select(r => new
        {
            r.Id,
            r.Rating,
            r.Comment,
            r.CreatedAt,
            CustomerName = r.CustomerUser.FullName
        });

        return Ok(ApiResponse<object>.Ok(result));
    }
}
