namespace Consultify.Web.Models;

public class Review
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public Guid CustomerUserId { get; set; }
    public int ConsultantProfileId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Appointment Appointment { get; set; } = null!;
    public ApplicationUser CustomerUser { get; set; } = null!;
    public ConsultantProfile ConsultantProfile { get; set; } = null!;
}
