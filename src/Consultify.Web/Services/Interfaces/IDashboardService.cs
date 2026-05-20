using Consultify.Web.Models;

namespace Consultify.Web.Services.Interfaces;

public class AdminDashboardStats
{
    public int TotalUsers { get; set; }
    public int TotalConsultants { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalAppointments { get; set; }
    public Dictionary<AppointmentStatus, int> AppointmentsByStatus { get; set; } = [];
    public List<Appointment> RecentAppointments { get; set; } = [];
}

public class ConsultantDashboardStats
{
    public List<Appointment> UpcomingAppointments { get; set; } = [];
    public int TodayCount { get; set; }
    public int WeeklyCount { get; set; }
    public int TotalAppointments { get; set; }
    public double AverageRating { get; set; }
}

public interface IDashboardService
{
    Task<AdminDashboardStats> GetAdminStatsAsync();
    Task<ConsultantDashboardStats> GetConsultantStatsAsync(Guid userId);
}
