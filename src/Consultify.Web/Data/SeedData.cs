using Microsoft.AspNetCore.Identity;
using Consultify.Web.Models;

namespace Consultify.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        string[] roleNames = ["Admin", "Consultant", "Customer"];
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        var adminEmail = "admin@consultify.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Admin",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        var consultants = new[]
        {
            new { Email = "sarah.chen@consultify.com", FirstName = "Sarah", LastName = "Chen", Bio = "Experienced career coach helping professionals navigate career transitions.", Specialization = "Career Coaching", Rate = 120m, Years = 8 },
            new { Email = "marcus.johnson@consultify.com", FirstName = "Marcus", LastName = "Johnson", Bio = "Strategic advisor with expertise in business growth and market expansion.", Specialization = "Business Strategy", Rate = 150m, Years = 12 },
            new { Email = "priya.patel@consultify.com", FirstName = "Priya", LastName = "Patel", Bio = "Certified mental wellness practitioner focused on stress management and mindfulness.", Specialization = "Mental Wellness", Rate = 100m, Years = 6 }
        };

        foreach (var c in consultants)
        {
            if (await userManager.FindByEmailAsync(c.Email) == null)
            {
                var consultant = new ApplicationUser
                {
                    UserName = c.Email,
                    Email = c.Email,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(consultant, "Consult123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(consultant, "Consultant");

                    var profile = new ConsultantProfile
                    {
                        UserId = consultant.Id,
                        Bio = c.Bio,
                        Specialization = c.Specialization,
                        HourlyRate = c.Rate,
                        YearsOfExperience = c.Years,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.ConsultantProfiles.Add(profile);
                    await context.SaveChangesAsync();

                    var today = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc);
                    for (int day = 1; day <= 7; day++)
                    {
                        var date = today.AddDays(day);
                        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                            continue;

                        var startTime = date.AddHours(9);
                        var endTime = date.AddHours(12);

                        var current = startTime;
                        while (current < endTime)
                        {
                            var slot = new TimeSlot
                            {
                                ConsultantProfileId = profile.Id,
                                StartTime = current,
                                EndTime = current.AddMinutes(30),
                                IsBooked = false
                            };
                            context.TimeSlots.Add(slot);
                            current = current.AddMinutes(30);
                        }
                    }
                }
            }
        }

        var customerEmail = "alice@example.com";
        if (await userManager.FindByEmailAsync(customerEmail) == null)
        {
            var customer = new ApplicationUser
            {
                UserName = customerEmail,
                Email = customerEmail,
                FirstName = "Alice",
                LastName = "Johnson",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(customer, "Customer123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(customer, "Customer");
            }
        }

        var customer2Email = "bob@example.com";
        if (await userManager.FindByEmailAsync(customer2Email) == null)
        {
            var customer2 = new ApplicationUser
            {
                UserName = customer2Email,
                Email = customer2Email,
                FirstName = "Bob",
                LastName = "Smith",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(customer2, "Customer123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(customer2, "Customer");
            }
        }

        await context.SaveChangesAsync();
    }
}
