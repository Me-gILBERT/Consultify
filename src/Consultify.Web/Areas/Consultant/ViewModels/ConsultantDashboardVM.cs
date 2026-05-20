using Consultify.Web.Models;

namespace Consultify.Web.Areas.Consultant.ViewModels;

public class ConsultantDashboardVM
{
    public List<Appointment> UpcomingAppointments { get; set; } = [];
    public int TodayCount { get; set; }
    public int WeeklyCount { get; set; }
    public int TotalAppointments { get; set; }
    public double AverageRating { get; set; }
}

public class CreateTimeSlotVM
{
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
    public TimeSpan StartTime { get; set; } = new(9, 0, 0);
    public TimeSpan EndTime { get; set; } = new(17, 0, 0);
}

public class ConsultantProfileVM
{
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public decimal? HourlyRate { get; set; }
    public int? YearsOfExperience { get; set; }
}
