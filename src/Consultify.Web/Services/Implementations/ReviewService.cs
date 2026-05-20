using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;

namespace Consultify.Web.Services.Implementations;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;

    public ReviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Review>> GetReviewsForConsultantAsync(int consultantProfileId, int page, int pageSize)
    {
        return await _context.Reviews
            .Include(r => r.CustomerUser)
            .Where(r => r.ConsultantProfileId == consultantProfileId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<double> GetAverageRatingAsync(int consultantProfileId)
    {
        return await _context.Reviews
            .Where(r => r.ConsultantProfileId == consultantProfileId)
            .AverageAsync(r => (double?)r.Rating) ?? 0;
    }

    public async Task<int> GetReviewCountAsync(int consultantProfileId)
    {
        return await _context.Reviews
            .CountAsync(r => r.ConsultantProfileId == consultantProfileId);
    }

    public async Task<Review> SubmitReviewAsync(int appointmentId, Guid customerUserId, int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var appointment = await _context.Appointments
            .Include(a => a.Review)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Appointment not found.");

        if (appointment.CustomerUserId != customerUserId)
            throw new InvalidOperationException("You can only review your own appointments.");

        if (appointment.Status != AppointmentStatus.Completed)
            throw new InvalidOperationException("You can only review completed appointments.");

        if (appointment.Review != null)
            throw new InvalidOperationException("You have already reviewed this appointment.");

        var review = new Review
        {
            AppointmentId = appointmentId,
            CustomerUserId = customerUserId,
            ConsultantProfileId = appointment.ConsultantProfileId,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    public async Task<bool> DeleteReviewAsync(int reviewId)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null) return false;

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return true;
    }
}
