using Consultify.Web.Models;

namespace Consultify.Web.Services.Interfaces;

public interface IAdminService
{
    Task<int> GetTotalUsersAsync();
    Task<int> GetTotalConsultantsAsync();
    Task<int> GetTotalCustomersAsync();
    Task<int> GetTotalAppointmentsAsync();
    Task<Dictionary<AppointmentStatus, int>> GetAppointmentsByStatusAsync();
    Task<List<ApplicationUser>> GetUsersAsync(string? role, string? search, int page, int pageSize);
    Task<ApplicationUser?> GetUserByIdAsync(Guid id);
    Task<bool> ToggleUserStatusAsync(Guid userId);
    Task<List<Appointment>> GetAllAppointmentsAsync(AppointmentStatus? statusFilter);
    Task<bool> CancelAppointmentAsync(int appointmentId, string reason);
    Task<List<Review>> GetAllReviewsAsync(int page, int pageSize);
    Task<bool> DeleteReviewAsync(int reviewId);
}
