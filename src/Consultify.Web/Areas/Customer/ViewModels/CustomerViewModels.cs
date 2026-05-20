using Consultify.Web.Models;

namespace Consultify.Web.Areas.Customer.ViewModels;

public class BrowseConsultantsVM
{
    public List<ConsultantCardVM> Consultants { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? SpecializationFilter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public class ConsultantCardVM
{
    public int ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public decimal? HourlyRate { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class ConsultantDetailVM
{
    public int ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public decimal? HourlyRate { get; set; }
    public int? YearsOfExperience { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public Dictionary<DateTime, List<TimeSlot>> AvailableSlots { get; set; } = [];
    public List<ReviewDisplayVM> Reviews { get; set; } = [];
}

public class ReviewDisplayVM
{
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BookConfirmVM
{
    public int TimeSlotId { get; set; }
    public string ConsultantName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Notes { get; set; }
}
