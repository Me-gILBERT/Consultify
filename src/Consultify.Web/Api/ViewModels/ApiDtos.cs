namespace Consultify.Web.Api.ViewModels;

public class SlotRequestDto
{
    public int ConsultantProfileId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool AutoSplit { get; set; } = true;
}

public class SlotResponseDto
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsBooked { get; set; }
}

public class BookingRequestDto
{
    public int TimeSlotId { get; set; }
    public string? Notes { get; set; }
}

public class AppointmentResponseDto
{
    public int Id { get; set; }
    public string ConsultantName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ReviewRequestDto
{
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
