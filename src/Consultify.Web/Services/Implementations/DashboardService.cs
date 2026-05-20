using Microsoft.EntityFrameworkCore;
using Consultify.Web.Data;
using Consultify.Web.Models;
using Consultify.Web.Services.Interfaces;

namespace Consultify.Web.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardStats> GetAdminStatsAsync()
    {
        var roleConsultant = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Consultant");
        var roleCustomer = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");

        var consultantCount = roleConsultant != null
            ? await _context.UserRoles.CountAsync(ur => ur.RoleId == roleConsultant.Id)
            : 0;

        var customerCount = roleCustomer != null
            ? await _context.UserRoles.CountAsync(ur => ur.RoleId == roleCustomer.Id)
            : 0;

        var appointmentsByStatus = await _context.Appointments
            .GroupBy(a => a.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        var recentAppointments = await _context.Appointments
            .Include(a => a.TimeSlot)
            .Include(a => a.CustomerUser)
            .Include(a => a.ConsultantProfile).ThenInclude(cp => cp.User)
            .OrderByDescending(a => a.BookedAt)
            .Take(10)
            .ToListAsync();

        return new AdminDashboardStats
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalConsultants = consultantCount,
            TotalCustomers = customerCount,
            TotalAppointments = await _context.Appointments.CountAsync(),
            AppointmentsByStatus = appointmentsByStatus,
            RecentAppointments = recentAppointments
        };
    }

    public async Task<ConsultantDashboardStats> GetConsultantStatsAsync(Guid userId)
    {
        var profile = await _context.ConsultantProfiles
            .FirstOrDefaultAsync(cp => cp.UserId == userId);

        if (profile == null)
            return new ConsultantDashboardStats();

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);

        var appointments = await _context.Appointments
            .Include(a => a.TimeSlot)
            .Include(a => a.CustomerUser)
            .Include(a => a.Review)
            .Where(a => a.ConsultantProfileId == profile.Id)
            .ToListAsync();

        var averageRating = appointments
            .Where(a => a.Review != null)
            .Select(a => a.Review!.Rating)
            .DefaultIfEmpty()
            .Average();

        return new ConsultantDashboardStats
        {
            UpcomingAppointments = appointments
                .Where(a => a.Status == AppointmentStatus.Scheduled && a.TimeSlot.StartTime > now)
                .OrderBy(a => a.TimeSlot.StartTime)
                .Take(5)
                .ToList(),
            TodayCount = appointments.Count(a => a.TimeSlot.StartTime.Date == todayStart),
            WeeklyCount = appointments.Count(a => a.TimeSlot.StartTime >= weekStart),
            TotalAppointments = appointments.Count,
            AverageRating = averageRating
        };
    }
}
