using Consultify.Web.Models;

namespace Consultify.Web.Areas.Admin.ViewModels;

public class DashboardStatsVM
{
    public int TotalUsers { get; set; }
    public int TotalConsultants { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalAppointments { get; set; }
    public Dictionary<AppointmentStatus, int> AppointmentsByStatus { get; set; } = [];
    public List<Appointment> RecentAppointments { get; set; } = [];
}

public class UserListVM
{
    public List<ApplicationUser> Users { get; set; } = [];
    public string? RoleFilter { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public class UserDetailVM
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
}

public class AppointmentListVM
{
    public List<Appointment> Appointments { get; set; } = [];
    public AppointmentStatus? StatusFilter { get; set; }
}

public class ReviewListVM
{
    public List<Review> Reviews { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}
