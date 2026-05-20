namespace Consultify.Web.Models;

public class ConsultantProfile
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public decimal? HourlyRate { get; set; }
    public int? YearsOfExperience { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
