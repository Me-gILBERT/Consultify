using Consultify.Web.Models;

namespace Consultify.Web.Services.Interfaces;

public interface IReviewService
{
    Task<List<Review>> GetReviewsForConsultantAsync(int consultantProfileId, int page, int pageSize);
    Task<double> GetAverageRatingAsync(int consultantProfileId);
    Task<int> GetReviewCountAsync(int consultantProfileId);
    Task<Review> SubmitReviewAsync(int appointmentId, Guid customerUserId, int rating, string? comment);
    Task<bool> DeleteReviewAsync(int reviewId);
}
