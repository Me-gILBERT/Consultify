using Microsoft.AspNetCore.Identity;

namespace Consultify.Web.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}";

    public ConsultantProfile? ConsultantProfile { get; set; }
    public ICollection<Appointment> CustomerAppointments { get; set; } = new List<Appointment>();
}
