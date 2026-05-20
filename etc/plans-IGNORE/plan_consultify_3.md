# Consultify — MVC Architecture Plan

## Tech Stack
- **Framework:** ASP.NET Core MVC (C#) with Razor Views
- **Database:** PostgreSQL
- **Auth:** Cookie Authentication with ASP.NET Core Identity + Role-based Authorization
- **ORM:** Entity Framework Core
- **Roles:** Admin, Consultant, Customer

---

## 1. Solution Structure

```
Consultify/
├── Consultify.sln
└── src/
    └── Consultify.Web/                     # ASP.NET Core MVC App
        ├── Areas/
        │   ├── Admin/                      # /Admin/*
        │   │   ├── Controllers/
        │   │   ├── Views/
        │   │   └── ViewModels/
        │   ├── Consultant/                 # /Consultant/*
        │   │   ├── Controllers/
        │   │   ├── Views/
        │   │   └── ViewModels/
        │   └── Customer/                   # /Customer/*
        │       ├── Controllers/
        │       ├── Views/
        │       └── ViewModels/
        ├── Controllers/                    # Shared: HomeController, AccountController
        ├── Views/                          # Shared: Layout, _LoginPartial, Error
        ├── Models/                         # Domain entities
        ├── ViewModels/                     # View-specific DTOs (shared ones)
        ├── Services/                       # Business logic (interfaces + implementations)
        │   ├── Interfaces/
        │   └── Implementations/
        ├── Data/                           # AppDbContext, Migrations, SeedData
        │   └── Configurations/             # Entity type configurations (IEntityTypeConfiguration<T>)
        ├── wwwroot/                        # CSS, JS, lib, images
        ├── Program.cs
        ├── appsettings.json
        └── appsettings.Development.json
├── tests/
│   └── Consultify.Web.Tests/
└── Consultify.sln
```

---

## 2. Domain Models

### Extended IdentityUser
```
IdentityUser (built-in, GUID primary key)
├── FirstName              string
├── LastName               string
├── ProfilePictureUrl      string?
├── CreatedAt              DateTime (UTC)
└── IsActive               bool
```

### Roles (seeded via ASP.NET Core Identity)
- `Admin`
- `Consultant`
- `Customer`

### ConsultantProfile
| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| UserId | Guid (FK) | → IdentityUser.Id, unique |
| Bio | string? | |
| Specialization | string? | e.g. "Career Coaching", "Mental Health" |
| HourlyRate | decimal? | |
| YearsOfExperience | int? | |
| IsActive | bool | Default true |
| CreatedAt | DateTime | UTC |

### TimeSlot
| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| ConsultantProfileId | int (FK) | → ConsultantProfile.Id |
| StartTime | DateTime | UTC |
| EndTime | DateTime | UTC |
| IsBooked | bool | Default false |
| RowVersion | byte[] | Concurrency token |

### Appointment
| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| TimeSlotId | int (FK) | → TimeSlot.Id, unique index |
| CustomerUserId | Guid (FK) | → IdentityUser.Id |
| ConsultantProfileId | int (FK) | → ConsultantProfile.Id |
| Status | AppointmentStatus | Enum: Scheduled, Completed, Cancelled, NoShow |
| BookedAt | DateTime | UTC |
| CancelledAt | DateTime? | UTC |
| CancellationReason | string? | |
| Notes | string? | Customer's note when booking |

### Review
| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| AppointmentId | int (FK) | → Appointment.Id, unique |
| CustomerUserId | Guid (FK) | → IdentityUser.Id |
| ConsultantProfileId | int (FK) | → ConsultantProfile.Id |
| Rating | int | 1–5 |
| Comment | string? | |
| CreatedAt | DateTime | UTC |

---

## 3. Entity Relationships

```
IdentityUser (1) ── (1) ConsultantProfile
IdentityUser (1) ── (*) Appointment (as Customer)
ConsultantProfile (1) ── (*) TimeSlot
ConsultantProfile (1) ── (*) Appointment (as Consultant)
TimeSlot (1) ── (1) Appointment
Appointment (1) ── (0..1) Review
ConsultantProfile (1) ── (*) Review
```

---

## 4. Areas & Route Map

### Shared (no area)

| Route | View | Auth | Description |
|-------|------|------|-------------|
| `/` | Home/Index | Anonymous | Landing page with hero, how it works, featured consultants |
| `/Home/Privacy` | Home/Privacy | Anonymous | Privacy policy |
| `/Home/Error` | Home/Error | Anonymous | Error page |
| `/Account/Register` | Account/Register | Anonymous | Register as Consultant or Customer |
| `/Account/Login` | Account/Login | Anonymous | Sign in |
| `/Account/Logout` | — | Authenticated | Sign out |
| `/Account/AccessDenied` | Account/AccessDenied | Authenticated | Access denied page |

### Admin Area (`/Admin/*`)

| Route | View | Description |
|-------|------|-------------|
| `/Admin/Dashboard` | Dashboard | Stats: total users, consultants, appointments, reviews |
| `/Admin/Users` | Users/Index | List/search all users, filter by role |
| `/Admin/Users/Details/{id}` | Users/Details | View user info, appointments |
| `/Admin/Users/ToggleStatus/{id}` | — | Enable/disable user (POST) |
| `/Admin/Consultants` | Consultants/Index | List all consultants, their status |
| `/Admin/Appointments` | Appointments/Index | All appointments, filter by status/date/consultant |
| `/Admin/Reviews` | Reviews/Index | All reviews, delete inappropriate ones |

### Consultant Area (`/Consultant/*`)

| Route | View | Description |
|-------|------|-------------|
| `/Consultant/Dashboard` | Dashboard | Upcoming appointments count, quick stats |
| `/Consultant/Availability` | Availability/Index | Weekly calendar grid of time slots |
| `/Consultant/Availability/Create` | Availability/Create | Add new time slot(s) — date, start, end |
| `/Consultant/Availability/Delete/{id}` | — | Delete slot (only if not booked, POST) |
| `/Consultant/Appointments` | Appointments/Index | List all my appointments, filter by status |
| `/Consultant/Appointments/Details/{id}` | Appointments/Details | View appointment info |
| `/Consultant/Appointments/MarkCompleted/{id}` | — | Mark appointment as completed (POST) |
| `/Consultant/Profile` | Profile/Index | View/edit my profile (bio, rate, specialization) |
| `/Consultant/Profile/Edit` | Profile/Edit | Edit profile form |

### Customer Area (`/Customer/*`)

| Route | View | Description |
|-------|------|-------------|
| `/Customer/Dashboard` | Dashboard | Upcoming and past appointments summary |
| `/Customer/Consultants` | Consultants/Index | Browse/search/filter consultants |
| `/Customer/Consultants/Details/{id}` | Consultants/Details | Consultant profile + ratings |
| `/Customer/Book/{consultantId}` | Book/AvailableSlots | Calendar of available slots for a consultant |
| `/Customer/Book/Confirm/{slotId}` | Book/Confirm | Confirm booking details |
| `/Customer/Book/Confirm/{slotId}` (POST) | — | Finalize booking |
| `/Customer/Appointments` | Appointments/Index | My appointments list |
| `/Customer/Appointments/Details/{id}` | Appointments/Details | View appointment, cancel button if eligible |
| `/Customer/Appointments/Cancel/{id}` | Appointments/Cancel | Confirm cancellation |
| `/Customer/Appointments/Cancel/{id}` (POST) | — | Execute cancellation |
| `/Customer/Appointments/Review/{id}` | Appointments/Review | Leave a review (GET + POST) |

---

## 5. ViewModels

### Account
- **`RegisterViewModel`** — Email, Password, ConfirmPassword, FirstName, LastName, Role (dropdown: Consultant | Customer)
- **`LoginViewModel`** — Email, Password, RememberMe

### Home
- **`LandingViewModel`** — FeaturedConsultants (list of ConsultantCardViewModel)

### Shared
- **`ConsultantCardViewModel`** — Id, FullName, Specialization, HourlyRate, AverageRating, ReviewCount, ProfilePictureUrl
- **`PaginationViewModel`** — Page, PageSize, TotalCount, TotalPages

### Admin
- **`DashboardStatsViewModel`** — TotalUsers, TotalConsultants, TotalCustomers, TotalAppointments, AppointmentsByStatus (dictionary), RecentAppointments
- **`UserListViewModel`** — Users (paginated), RoleFilter, SearchTerm
- **`UserDetailViewModel`** — User info, role, registration date, appointment count
- **`AppointmentListViewModel`** — Appointments (paginated), StatusFilter, DateRange
- **`ReviewListViewModel`** — Reviews (paginated), search/filter

### Consultant
- **`ConsultantDashboardViewModel`** — UpcomingAppointments (next 5), TodayCount, WeeklyCount, TotalAppointments, AverageRating
- **`AvailabilityIndexViewModel`** — Slots grouped by date, with booking status
- **`CreateTimeSlotViewModel`** — Date, StartTime, EndTime, (optionally) recurring weekly
- **`ConsultantProfileViewModel`** — Bio, Specialization, HourlyRate, YearsOfExperience, AverageRating, ReviewCount

### Customer
- **`BrowseConsultantsViewModel`** — Consultants (paginated), SearchTerm, SpecializationFilter, SortBy
- **`ConsultantDetailViewModel`** — ConsultantProfileViewModel + AvailableSlots (grouped by date) + Reviews list
- **`AvailableSlotsViewModel`** — Consultant name, Slots grouped by date (next 14 days)
- **`BookConfirmViewModel`** — Consultant name, Date, StartTime–EndTime, Notes field
- **`CustomerAppointmentListViewModel`** — Appointments (paginated), filter by upcoming/past
- **`AppointmentDetailViewModel`** — Full appointment info, cancellation eligibility, review button if completed
- **`CancelAppointmentViewModel`** — Appointment info, reason field
- **`ReviewFormViewModel`** — Appointment info, Rating (1–5 stars), Comment
- **`ReviewDisplayViewModel`** — ReviewerName, Rating, Comment, Date

---

## 6. Services Layer

### IBookingService
```csharp
Task<List<TimeSlot>> GetAvailableSlotsAsync(int consultantProfileId);
Task<Appointment> BookSlotAsync(int timeSlotId, Guid customerUserId, string? notes);
Task<Appointment?> CancelAppointmentAsync(int appointmentId, Guid userId, string? reason);
Task<Appointment?> ConfirmBookingAsync(int slotId, Guid customerUserId, string? notes);
```

### IConsultantService
```csharp
Task<ConsultantProfile?> GetProfileByUserIdAsync(Guid userId);
Task<ConsultantProfile?> GetProfileByIdAsync(int profileId);
Task UpdateProfileAsync(ConsultantProfile profile);
Task<List<TimeSlot>> GetTimeSlotsAsync(int consultantProfileId);
Task<TimeSlot> AddTimeSlotAsync(int consultantProfileId, DateTime start, DateTime end);
Task<bool> RemoveTimeSlotAsync(int slotId, int consultantProfileId);
Task<List<Appointment>> GetAppointmentsAsync(int consultantProfileId);
```

### IReviewService
```csharp
Task<List<Review>> GetReviewsForConsultantAsync(int consultantProfileId);
Task<double> GetAverageRatingAsync(int consultantProfileId);
Task<int> GetReviewCountAsync(int consultantProfileId);
Task<Review> SubmitReviewAsync(int appointmentId, Guid customerUserId, int rating, string? comment);
Task<bool> DeleteReviewAsync(int reviewId, Guid adminUserId);
```

### IAdminService
```csharp
Task<DashboardStatsViewModel> GetDashboardStatsAsync();
Task<List<IdentityUser>> GetUsersAsync(string? role, string? search, int page, int pageSize);
Task<IdentityUser?> GetUserByIdAsync(Guid id);
Task<bool> ToggleUserStatusAsync(Guid userId);
Task<List<Appointment>> GetAllAppointmentsAsync(AppointmentStatus? status);
Task<bool> DeleteReviewAsync(int reviewId);
```

### IDashboardService
```csharp
Task<DashboardStatsViewModel> GetStatsAsync();                         // Admin
Task<ConsultantDashboardViewModel> GetConsultantStatsAsync(Guid userId);
Task<CustomerDashboardViewModel> GetCustomerStatsAsync(Guid userId);   // (inline in controller if small)
```

---

## 7. Key Business Rules

1. **Double-booking prevention** — TimeSlot has `RowVersion` (EF concurrency token); booking fails if slot was already taken between read and write.
2. **Slot deletion** — Consultant can only delete a TimeSlot where `IsBooked == false`.
3. **Cancellation window** — Customer can cancel up to 24 hours before `StartTime`. After that, they must contact the consultant/admin.
4. **Reviews** — Only on appointments with `Status == Completed`. One review per appointment (unique constraint on `AppointmentId`).
5. **Rating range** — 1–5 inclusive, validated on the server.
6. **Profile activation** — Admin can deactivate any user. Deactivated users cannot log in (custom `UserClaimsPrincipalFactory` or sign-in check).
7. **Registration** — New user picks a role (Consultant or Customer) on sign-up. Admin accounts are created only via seeding.

---

## 8. Seed Data

| Entity | Details |
|--------|---------|
| **Admin user** | Email: `admin@consultify.com`, Password: `Admin123!`, Role: Admin |
| **Consultant 1** | "Sarah Chen", Career Coaching, $120/hr, 8 yrs exp |
| **Consultant 2** | "Marcus Johnson", Business Strategy, $150/hr, 12 yrs exp |
| **Consultant 3** | "Priya Patel", Mental Wellness, $100/hr, 6 yrs exp |
| **TimeSlots** | Each consultant gets 3–5 slots per day for the next 7 days |
| **Appointments** | 2–3 sample appointments (mix of Scheduled and Completed) |
| **Reviews** | 1–2 reviews on Completed appointments |

---

## 9. NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity with EF Core |
| `Microsoft.AspNetCore.Identity.UI` | Identity UI pages (scaffold or custom) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider for EF Core |
| `Microsoft.EntityFrameworkCore.Tools` | CLI migrations |
| `Microsoft.EntityFrameworkCore.Design` | Migration design-time support |

---

## 10. Implementation Order

| Phase | Tasks |
|-------|-------|
| **1. Scaffold** | `dotnet new mvc` with Individual Auth, add NuGet packages, configure PostgreSQL in `appsettings.json` and `Program.cs` |
| **2. Identity Setup** | Extend `IdentityUser` with custom properties, configure `AppDbContext` with Identity, seed roles + admin user, custom registration with role selection |
| **3. Domain Models** | Create `ConsultantProfile`, `TimeSlot`, `Appointment`, `Review` entities with FK relationships and configurations |
| **4. Migrations** | `dotnet ef migrations add InitialCreate`, then `dotnet ef database update` |
| **5. Consultant Area** | Scaffold area, implement availability CRUD (add/delete time slots), profile editing |
| **6. Customer Area** | Browse consultants, view profiles + ratings, view available slots, book appointment, cancel appointment |
| **7. Reviews** | Submit review on completed appointments, display ratings on consultant cards |
| **8. Admin Area** | Dashboard with stats, user management (list, search, toggle active), appointment overview, review moderation |
| **9. Polish** | Bootstrap UI, responsive layout, validation messages, error handling (404, 500), flash messages (TempData), loading states |

---

## 11. Database Schema (SQL)

```sql
-- AspNetUsers (extended by Identity)
CREATE TABLE "AspNetUsers" (
    "Id" UUID PRIMARY KEY,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "ProfilePictureUrl" TEXT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    -- plus all Identity columns (UserName, Email, PasswordHash, etc.)
);

-- ConsultantProfiles
CREATE TABLE "ConsultantProfiles" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL UNIQUE REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "Bio" TEXT NULL,
    "Specialization" TEXT NULL,
    "HourlyRate" DECIMAL(18,2) NULL,
    "YearsOfExperience" INT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- TimeSlots
CREATE TABLE "TimeSlots" (
    "Id" SERIAL PRIMARY KEY,
    "ConsultantProfileId" INT NOT NULL REFERENCES "ConsultantProfiles"("Id") ON DELETE CASCADE,
    "StartTime" TIMESTAMPTZ NOT NULL,
    "EndTime" TIMESTAMPTZ NOT NULL,
    "IsBooked" BOOLEAN NOT NULL DEFAULT FALSE,
    "RowVersion" BYTEA NOT NULL DEFAULT '\\x0000000000000000'
);
CREATE INDEX "IX_TimeSlots_ConsultantProfileId" ON "TimeSlots"("ConsultantProfileId");
CREATE INDEX "IX_TimeSlots_StartTime" ON "TimeSlots"("StartTime");

-- Appointments
CREATE TABLE "Appointments" (
    "Id" SERIAL PRIMARY KEY,
    "TimeSlotId" INT NOT NULL UNIQUE REFERENCES "TimeSlots"("Id") ON DELETE RESTRICT,
    "CustomerUserId" UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "ConsultantProfileId" INT NOT NULL REFERENCES "ConsultantProfiles"("Id") ON DELETE RESTRICT,
    "Status" INT NOT NULL DEFAULT 0,  -- 0=Scheduled, 1=Completed, 2=Cancelled, 3=NoShow
    "BookedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CancelledAt" TIMESTAMPTZ NULL,
    "CancellationReason" TEXT NULL,
    "Notes" TEXT NULL
);
CREATE INDEX "IX_Appointments_CustomerUserId" ON "Appointments"("CustomerUserId");
CREATE INDEX "IX_Appointments_ConsultantProfileId" ON "Appointments"("ConsultantProfileId");
CREATE INDEX "IX_Appointments_Status" ON "Appointments"("Status");

-- Reviews
CREATE TABLE "Reviews" (
    "Id" SERIAL PRIMARY KEY,
    "AppointmentId" INT NOT NULL UNIQUE REFERENCES "Appointments"("Id") ON DELETE CASCADE,
    "CustomerUserId" UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "ConsultantProfileId" INT NOT NULL REFERENCES "ConsultantProfiles"("Id") ON DELETE CASCADE,
    "Rating" INT NOT NULL CHECK ("Rating" >= 1 AND "Rating" <= 5),
    "Comment" TEXT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX "IX_Reviews_ConsultantProfileId" ON "Reviews"("ConsultantProfileId");
```

---

## 12. Key Concurrency Pattern (Double-Booking Prevention)

```csharp
// BookingService.BookSlotAsync
public async Task<Appointment> BookSlotAsync(int timeSlotId, Guid customerUserId)
{
    using var strategy = _context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        var slot = await _context.TimeSlots
            .FirstOrDefaultAsync(s => s.Id == timeSlotId);

        if (slot == null || slot.IsBooked)
            throw new InvalidOperationException("Slot is not available.");

        slot.IsBooked = true;

        var appointment = new Appointment
        {
            TimeSlotId = slot.Id,
            CustomerUserId = customerUserId,
            ConsultantProfileId = slot.ConsultantProfileId,
            Status = AppointmentStatus.Scheduled,
            BookedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);

        try
        {
            await _context.SaveChangesAsync();  // may throw DbUpdateConcurrencyException
            await tx.CommitAsync();
            return appointment;
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException("This slot was just booked by someone else. Please choose another.");
        }
    });
}
```
