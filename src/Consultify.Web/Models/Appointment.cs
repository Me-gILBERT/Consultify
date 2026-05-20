namespace Consultify.Web.Models;

public enum AppointmentStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2,
    NoShow = 3
}

public class Appointment
{
    public int Id { get; set; }
    public int TimeSlotId { get; set; }
    public Guid CustomerUserId { get; set; }
    public int ConsultantProfileId { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public DateTime BookedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }

    public TimeSlot TimeSlot { get; set; } = null!;
    public ApplicationUser CustomerUser { get; set; } = null!;
    public ConsultantProfile ConsultantProfile { get; set; } = null!;
    public Review? Review { get; set; }
}
