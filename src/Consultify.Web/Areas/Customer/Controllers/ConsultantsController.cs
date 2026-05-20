using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Services.Interfaces;
using Consultify.Web.Areas.Customer.ViewModels;

namespace Consultify.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class ConsultantsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IReviewService _reviewService;

    public ConsultantsController(ApplicationDbContext context, IReviewService reviewService)
    {
        _context = context;
        _reviewService = reviewService;
    }

    public async Task<IActionResult> Index(string? search, string? specialization, int page = 1)
    {
        var query = _context.ConsultantProfiles
            .Include(cp => cp.User)
            .Where(cp => cp.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(cp => cp.User.FirstName.ToLower().Contains(term)
                || cp.User.LastName.ToLower().Contains(term)
                || (cp.Specialization != null && cp.Specialization.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(specialization))
        {
            query = query.Where(cp => cp.Specialization != null && cp.Specialization == specialization);
        }

        var pageSize = 12;
        var total = await query.CountAsync();
        var consultants = await query
            .OrderBy(cp => cp.User.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var cards = new List<ConsultantCardVM>();
        foreach (var cp in consultants)
        {
            cards.Add(new ConsultantCardVM
            {
                ProfileId = cp.Id,
                FullName = cp.User.FullName,
                Specialization = cp.Specialization,
                HourlyRate = cp.HourlyRate,
                AverageRating = await _reviewService.GetAverageRatingAsync(cp.Id),
                ReviewCount = await _reviewService.GetReviewCountAsync(cp.Id)
            });
        }

        var specializations = await _context.ConsultantProfiles
            .Where(cp => cp.Specialization != null)
            .Select(cp => cp.Specialization!)
            .Distinct()
            .ToListAsync();

        ViewBag.Specializations = specializations;
        ViewBag.CurrentSpecialization = specialization;

        var vm = new BrowseConsultantsVM
        {
            Consultants = cards,
            SearchTerm = search,
            SpecializationFilter = specialization,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var profile = await _context.ConsultantProfiles
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.Id == id);

        if (profile == null) return NotFound();

        var now = DateTime.UtcNow;
        var slots = await _context.TimeSlots
            .Where(ts => ts.ConsultantProfileId == id && !ts.IsBooked && ts.StartTime > now)
            .OrderBy(ts => ts.StartTime)
            .ToListAsync();

        var groupedSlots = slots
            .Where(s => s.StartTime <= now.AddDays(14))
            .GroupBy(s => s.StartTime.Date)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartTime).ToList());

        var reviews = await _reviewService.GetReviewsForConsultantAsync(id, 1, 10);

        var reviewVms = reviews.Select(r => new ReviewDisplayVM
        {
            CustomerName = r.CustomerUser.FullName,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();

        var vm = new ConsultantDetailVM
        {
            ProfileId = profile.Id,
            FullName = profile.User.FullName,
            Bio = profile.Bio,
            Specialization = profile.Specialization,
            HourlyRate = profile.HourlyRate,
            YearsOfExperience = profile.YearsOfExperience,
            AverageRating = await _reviewService.GetAverageRatingAsync(profile.Id),
            ReviewCount = await _reviewService.GetReviewCountAsync(profile.Id),
            AvailableSlots = groupedSlots,
            Reviews = reviewVms
        };

        return View(vm);
    }
}
