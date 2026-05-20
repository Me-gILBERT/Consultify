using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;

namespace Consultify.Web.Services.Implementations;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private const int DefaultPageSize = 20;

    public AdminService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<int> GetTotalUsersAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<int> GetTotalConsultantsAsync()
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Consultant");
        if (role == null) return 0;
        return await _context.UserRoles.CountAsync(ur => ur.RoleId == role.Id);
    }

    public async Task<int> GetTotalCustomersAsync()
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
        if (role == null) return 0;
        return await _context.UserRoles.CountAsync(ur => ur.RoleId == role.Id);
    }

    public async Task<int> GetTotalAppointmentsAsync()
    {
        return await _context.Appointments.CountAsync();
    }

    public async Task<Dictionary<AppointmentStatus, int>> GetAppointmentsByStatusAsync()
    {
        return await _context.Appointments
            .GroupBy(a => a.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task<List<ApplicationUser>> GetUsersAsync(string? role, string? search, int page, int pageSize)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => r.Name == role);
            if (roleEntity != null)
            {
                var userIds = _context.UserRoles
                    .Where(ur => ur.RoleId == roleEntity.Id)
                    .Select(ur => ur.UserId);
                query = query.Where(u => userIds.Contains(u.Id));
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u => u.Email!.ToLower().Contains(search)
                || u.FirstName.ToLower().Contains(search)
                || u.LastName.ToLower().Contains(search));
        }

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<bool> ToggleUserStatusAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Appointment>> GetAllAppointmentsAsync(AppointmentStatus? statusFilter)
    {
        var query = _context.Appointments
            .Include(a => a.TimeSlot)
            .Include(a => a.CustomerUser)
            .Include(a => a.ConsultantProfile).ThenInclude(cp => cp.User)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);

        return await query
            .OrderByDescending(a => a.BookedAt)
            .ToListAsync();
    }

    public async Task<bool> CancelAppointmentAsync(int appointmentId, string reason)
    {
        var appointment = await _context.Appointments
            .Include(a => a.TimeSlot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null) return false;

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.CancellationReason = reason;
        appointment.TimeSlot.IsBooked = false;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Review>> GetAllReviewsAsync(int page, int pageSize)
    {
        return await _context.Reviews
            .Include(r => r.CustomerUser)
            .Include(r => r.ConsultantProfile).ThenInclude(cp => cp.User)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
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
