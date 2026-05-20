namespace Consultify.Web.Models;

public class TimeSlot
{
    public int Id { get; set; }
    public int ConsultantProfileId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsBooked { get; set; } = false;

    public ConsultantProfile ConsultantProfile { get; set; } = null!;
    public Appointment? Appointment { get; set; }
}
