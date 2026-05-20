# Consultify — Final Architecture Plan

## Overview
Consultation booking platform with three roles: **Admin**, **Consultant**, and **Customer**. Consultants set available 30-min time slots (auto-split from date/time ranges), customers browse and book consultations.

---

## Tech Stack

| Layer | Choice |
|---|---|
| Framework | ASP.NET Core 8 MVC |
| Database | PostgreSQL via EF Core + Npgsql |
| Auth | ASP.NET Core Identity with Roles |
| UI | Razor Views + Bootstrap 5 |
| Validation | Data Annotations |

---

## 1. Solution Structure

```
Consultify/
├── Consultify.sln
└── src/
    └── Consultify.Web/
        ├── Areas/
        │   ├── Admin/
        │   │   ├── Controllers/
        │   │   │   └── AdminController.cs
        │   │   ├── Views/
        │   │   │   ├── Dashboard.cshtml
        │   │   │   ├── Users.cshtml
        │   │   │   └── Consultations.cshtml
        │   │   └── ViewModels/
        │   │       ├── DashboardStatsVM.cs
        │   │       ├── UserListVM.cs
        │   │       └── ConsultationListVM.cs
        │   ├── Consultant/
        │   │   ├── Controllers/
        │   │   │   ├── DashboardController.cs
        │   │   │   ├── AvailabilityController.cs
        │   │   │   ├── ConsultationsController.cs
        │   │   │   └── ProfileController.cs
        │   │   ├── Views/
        │   │   │   ├── Dashboard/
        │   │   │   ├── Availability/
        │   │   │   ├── Consultations/
        │   │   │   └── Profile/
        │   │   └── ViewModels/
        │   │       ├── ConsultantDashboardVM.cs
        │   │       ├── CreateTimeSlotVM.cs
        │   │       └── ConsultantProfileVM.cs
        │   └── Customer/
        │       ├── Controllers/
        │       │   ├── DashboardController.cs
        │       │   ├── ConsultantsController.cs
        │       │   ├── BookingController.cs
        │       │   └── ReviewsController.cs
        │       ├── Views/
        │       │   ├── Dashboard/
        │       │   ├── Consultants/
        │       │   ├── Booking/
        │       │   └── Reviews/
        │       └── ViewModels/
        │           ├── BrowseConsultantsVM.cs
        │           ├── ConsultantDetailVM.cs
        │           ├── BookConfirmVM.cs
        │           └── ReviewFormVM.cs
        ├── Controllers/
        │   ├── HomeController.cs
        │   └── AccountController.cs
        ├── Views/
        │   ├── Home/
        │   └── Shared/
        │       ├── _Layout.cshtml
        │       └── _LoginPartial.cshtml
        ├── Models/
        │   ├── ApplicationUser.cs
        │   ├── ConsultantProfile.cs
        │   ├── TimeSlot.cs
        │   ├── Consultation.cs
        │   └── Review.cs
        ├── Services/
        │   ├── Interfaces/
        │   │   ├── IConsultantService.cs
        │   │   ├── ITimeSlotService.cs
        │   │   ├── IBookingService.cs
        │   │   ├── IReviewService.cs
        │   │   └── IAdminService.cs
        │   └── Implementations/
        ├── Data/
        │   ├── AppDbContext.cs
        │   ├── Configurations/
        │   ├── Migrations/
        │   └── SeedData.cs
        ├── wwwroot/
        │   ├── css/
        │   ├── js/
        │   └── lib/
        ├── Program.cs
        ├── appsettings.json
        └── appsettings.Development.json
├── tests/
│   └── Consultify.Web.Tests/
```

---

## 2. Domain Models

### ApplicationUser (extends IdentityUser\<Guid\>)

| Property | Type | Notes |
|----------|------|-------|
| FirstName | string | |
| LastName | string | |
| FullName | computed | $"{FirstName} {LastName}" |
| ProfilePictureUrl | string? | |
| IsActive | bool | Default true. Deactivated users blocked on login |
| CreatedAt | DateTime | UTC |

### ConsultantProfile

| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| UserId | Guid (FK) | → ApplicationUser.Id, unique index |
| Bio | string? | Free-text |
| Specialization | string? | e.g. "Career Coaching", "Business Strategy" |
| HourlyRate | decimal(18,2)? | |
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
| RowVersion | byte[] | Concurrency token — prevents double-booking |

### Consultation

| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| TimeSlotId | int (FK) | → TimeSlot.Id, unique index |
| CustomerUserId | Guid (FK) | → ApplicationUser.Id |
| ConsultantProfileId | int (FK) | → ConsultantProfile.Id |
| Status | ConsultationStatus | Enum: Scheduled, Completed, Cancelled |
| Notes | string? | Customer's note at booking time |
| BookedAt | DateTime | UTC |
| CancelledAt | DateTime? | UTC |
| CancellationReason | string? | |

### Review

| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| ConsultationId | int (FK) | → Consultation.Id, unique |
| CustomerUserId | Guid (FK) | → ApplicationUser.Id |
| ConsultantProfileId | int (FK) | → ConsultantProfile.Id |
| Rating | int | 1–5, validated server-side |
| Comment | string? | |
| CreatedAt | DateTime | UTC |

### Roles (seeded via Identity)
- `Admin` — system oversight
- `Consultant` — sets availability, manages bookings
- `Customer` — browses, books, reviews

---

## 3. Entity Relationships

```
ApplicationUser (1) ── (1) ConsultantProfile
ApplicationUser (1) ── (*) Consultation (as Customer)

ConsultantProfile (1) ── (*) TimeSlot
ConsultantProfile (1) ── (*) Consultation (as Consultant)
ConsultantProfile (1) ── (*) Review

TimeSlot (1) ── (1) Consultation

Consultation (1) ── (0..1) Review
```

---

## 4. Areas & Route Map

### Shared (no area)

| Route | View | Auth | Description |
|-------|------|------|-------------|
| `/` | Home/Index | Anonymous | Landing page: hero, how it works, featured consultants |
| `/Home/Privacy` | Home/Privacy | Anonymous | Privacy policy |
| `/Home/Error` | Home/Error | Anonymous | Error page |
| `/Account/Register` | Account/Register | Anonymous | Register as Consultant or Customer |
| `/Account/Login` | Account/Login | Anonymous | Sign in |
| `/Account/Logout` | — | Authenticated | Sign out |
| `/Account/AccessDenied` | Account/AccessDenied | Authenticated | Access denied |

### Admin Area (`/Admin`)

| Route | View | Description |
|-------|------|-------------|
| `/Admin/Dashboard` | Dashboard | Stats: total users, consultants, consultations, reviews |
| `/Admin/Users` | Users/Index | List/search all users, filter by role |
| `/Admin/Users/Details/{id}` | Users/Details | View user info |
| `/Admin/Users/ToggleStatus/{id}` | — | Enable/disable user (POST) |
| `/Admin/Consultations` | Consultations/Index | All consultations, filter by status/date/consultant |
| `/Admin/Reviews` | Reviews/Index | All reviews, delete inappropriate ones |

### Consultant Area (`/Consultant`)

| Route | View | Description |
|-------|------|-------------|
| `/Consultant/Dashboard` | Dashboard | Upcoming consultations count, quick stats |
| `/Consultant/Availability` | Availability/Index | Weekly calendar grid of time slots |
| `/Consultant/Availability/Create` | Availability/Create | Batch create: pick date + start + end time → auto-splits into 30-min slots |
| `/Consultant/Availability/Delete/{id}` | — | Delete slot (only if not booked, POST) |
| `/Consultant/Consultations` | Consultations/Index | List my consultations, filter by status |
| `/Consultant/Consultations/Details/{id}` | Consultations/Details | View consultation info |
| `/Consultant/Consultations/MarkCompleted/{id}` | — | Mark completed (POST) |
| `/Consultant/Profile` | Profile/Index | View profile |
| `/Consultant/Profile/Edit` | Profile/Edit | Edit bio, specialization, rate |

### Customer Area (`/Customer`)

| Route | View | Description |
|-------|------|-------------|
| `/Customer/Dashboard` | Dashboard | Upcoming and past consultations summary |
| `/Customer/Consultants` | Consultants/Index | Browse/search/filter consultants |
| `/Customer/Consultants/Details/{id}` | Consultants/Details | Consultant profile + ratings + available slots |
| `/Customer/Book/{consultantId}` | Book/AvailableSlots | Pick a date → see available 30-min slots |
| `/Customer/Book/Confirm/{slotId}` (GET) | Book/Confirm | Confirm booking details |
| `/Customer/Book/Confirm/{slotId}` (POST) | — | Finalize booking (concurrency-safe) |
| `/Customer/Consultations` | Consultations/Index | My consultations list |
| `/Customer/Consultations/Details/{id}` | Consultations/Details | View details + cancel if eligible |
| `/Customer/Consultations/Cancel/{id}` (GET+POST) | Consultations/Cancel | Cancel with reason |
| `/Customer/Reviews/Create/{consultationId}` (GET+POST) | Reviews/Create | Leave review after completed consultation |

---

## 5. ViewModels

### Account
- **RegisterVM** — Email, Password, ConfirmPassword, FirstName, LastName, Role (dropdown: Consultant | Customer)
- **LoginVM** — Email, Password, RememberMe

### Home
- **LandingVM** — FeaturedConsultants (list of ConsultantCardVM)

### Shared
- **ConsultantCardVM** — Id, FullName, Specialization, HourlyRate, AverageRating, ReviewCount, ProfilePictureUrl
- **PaginationVM** — Page, PageSize, TotalCount, TotalPages

### Admin
- **DashboardStatsVM** — TotalUsers, TotalConsultants, TotalCustomers, TotalConsultations, ConsultationsByStatus (dictionary), RecentConsultations
- **UserListVM** — Users (paginated), RoleFilter, SearchTerm
- **UserDetailVM** — User info, role, registration date, consultation count
- **ConsultationListVM** — Consultations (paginated), StatusFilter, DateRange

### Consultant
- **ConsultantDashboardVM** — UpcomingConsultations (next 5), TodayCount, WeeklyCount, TotalConsultations, AverageRating
- **AvailabilityIndexVM** — Slots grouped by date, with booking status
- **CreateTimeSlotVM** — Date, StartTime, EndTime (system splits into 30-min slots, preview shown before confirm)
- **ConsultantProfileVM** — Bio, Specialization, HourlyRate, YearsOfExperience, AverageRating, ReviewCount

### Customer
- **BrowseConsultantsVM** — Consultants (paginated), SearchTerm, SpecializationFilter, SortBy
- **ConsultantDetailVM** — ConsultantProfileVM + AvailableSlots (grouped by date for next 14 days) + Reviews list
- **BookConfirmVM** — ConsultantName, Date, StartTime–EndTime, Notes field
- **CustomerConsultationListVM** — Consultations (paginated), filter by upcoming/past
- **ConsultationDetailVM** — Full consultation info, cancellation eligibility, review button if completed
- **CancelConsultationVM** — Consultation info, reason field
- **ReviewFormVM** — Consultation info, Rating (1–5 stars), Comment

---

## 6. Services Layer

### IBookingService
```csharp
Task<List<TimeSlot>> GetAvailableSlotsAsync(int consultantProfileId);
Task<Consultation> BookSlotAsync(int timeSlotId, Guid customerUserId, string? notes);
Task<Consultation?> CancelConsultationAsync(int consultationId, Guid userId, string? reason);
```

### IConsultantService
```csharp
Task<ConsultantProfile?> GetProfileByUserIdAsync(Guid userId);
Task<ConsultantProfile?> GetProfileByIdAsync(int profileId);
Task UpdateProfileAsync(ConsultantProfile profile);
Task<List<TimeSlot>> GetTimeSlotsAsync(int consultantProfileId);
Task<List<TimeSlot>> AddTimeSlotsBatchAsync(int consultantProfileId, DateOnly date, TimeOnly startTime, TimeOnly endTime);
Task<bool> RemoveTimeSlotAsync(int slotId, int consultantProfileId);
Task<List<Consultation>> GetConsultationsAsync(int consultantProfileId);
Task<bool> MarkCompletedAsync(int consultationId, int consultantProfileId);
```

### IReviewService
```csharp
Task<List<Review>> GetReviewsForConsultantAsync(int consultantProfileId);
Task<double> GetAverageRatingAsync(int consultantProfileId);
Task<int> GetReviewCountAsync(int consultantProfileId);
Task<Review> SubmitReviewAsync(int consultationId, Guid customerUserId, int rating, string? comment);
Task<bool> DeleteReviewAsync(int reviewId);
```

### IAdminService
```csharp
Task<DashboardStatsVM> GetDashboardStatsAsync();
Task<List<ApplicationUser>> GetUsersAsync(string? role, string? search, int page, int pageSize);
Task<ApplicationUser?> GetUserByIdAsync(Guid id);
Task<bool> ToggleUserStatusAsync(Guid userId);
Task<List<Consultation>> GetAllConsultationsAsync(ConsultationStatus? status);
Task<bool> DeleteReviewAsync(int reviewId);
```

---

## 7. Slot Creation Flow (Auto-split, 30-min)

1. Consultant navigates to `/Consultant/Availability/Create`
2. Fills form: **Date** + **Start Time** (e.g., 9:00 AM) + **End Time** (e.g., 12:00 PM)
3. On submit, `IConsultantService.AddTimeSlotsBatchAsync`:
   - Validates start < end, date is today or future
   - Generates 30-min intervals: `9:00-9:30`, `9:30-10:00`, `10:00-10:30`, `10:30-11:00`, `11:00-11:30`, `11:30-12:00`
   - Skips any slot whose time range has already passed
   - Skips any slot that overlaps with an existing (unbooked) slot — warns the consultant
   - Saves all new slots in a single transaction
4. Consultant sees the new slots in their weekly calendar at `/Consultant/Availability`

---

## 8. Booking Flow

1. Customer browses consultants at `/Customer/Consultants`
2. Clicks a consultant → `/Customer/Consultants/Details/{id}` shows profile + next 14 days of available slots grouped by date
3. Customer clicks a 30-min slot → `/Customer/Book/Confirm/{slotId}` (GET) shows confirmation with consultant name, date, time
4. Customer confirms → POST to `/Customer/Book/Confirm/{slotId}`:
   - `IBookingService.BookSlotAsync` is called
   - Uses `TimeSlot.RowVersion` concurrency token
   - If slot was already taken between display and confirm, `DbUpdateConcurrencyException` is caught and user is shown "Slot no longer available, please pick another"
   - On success: `TimeSlot.IsBooked = true`, `Consultation` created with `Status = Scheduled`
5. Consultant sees the booking on their dashboard
6. Customer sees it in `/Customer/Consultations`

---

## 9. Key Business Rules

1. **Double-booking prevention** — `TimeSlot` has `RowVersion` (EF concurrency token). `BookSlotAsync` catches `DbUpdateConcurrencyException` and returns a user-friendly error.
2. **Slot deletion** — Consultant can only delete a `TimeSlot` where `IsBooked == false`. Server-enforced.
3. **Cancellation window** — Customer can cancel up to 24 hours before `StartTime`. After that, they must contact support. Server-enforced.
4. **Reviews** — Only on consultations with `Status == Completed`. One review per consultation (unique constraint on `ConsultationId`).
5. **Rating range** — 1–5 inclusive, validated server-side.
6. **Profile deactivation** — Admin can toggle `IsActive`. Deactivated users see a message on their next login attempt (checked in custom sign-in or post-login middleware).
7. **Registration** — New user selects Consultant or Customer role on sign-up. Admin accounts are created only via seed data.
8. **Slot generation** — Must always land on clean 30-min boundaries (e.g., 9:00, 9:30, 10:00). Input start/end times that don't align to boundaries are rounded up/down respectively.

---

## 10. Database Schema (SQL)

```sql
-- AspNetUsers (extended by Identity)
CREATE TABLE "AspNetUsers" (
    "Id" UUID PRIMARY KEY,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "ProfilePictureUrl" TEXT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
    -- Identity columns: UserName, NormalizedUserName, Email,
    --   NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp,
    --   ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
    --   TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount
);

-- ConsultantProfiles
CREATE TABLE "ConsultantProfiles" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL UNIQUE
        REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
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
    "ConsultantProfileId" INT NOT NULL
        REFERENCES "ConsultantProfiles"("Id") ON DELETE CASCADE,
    "StartTime" TIMESTAMPTZ NOT NULL,
    "EndTime" TIMESTAMPTZ NOT NULL,
    "IsBooked" BOOLEAN NOT NULL DEFAULT FALSE,
    "RowVersion" BYTEA NOT NULL DEFAULT '\x0000000000000000'
);

CREATE INDEX "IX_TimeSlots_ConsultantProfileId"
    ON "TimeSlots"("ConsultantProfileId");
CREATE INDEX "IX_TimeSlots_StartTime"
    ON "TimeSlots"("StartTime");

-- Consultations
CREATE TABLE "Consultations" (
    "Id" SERIAL PRIMARY KEY,
    "TimeSlotId" INT NOT NULL UNIQUE
        REFERENCES "TimeSlots"("Id") ON DELETE RESTRICT,
    "CustomerUserId" UUID NOT NULL
        REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "ConsultantProfileId" INT NOT NULL
        REFERENCES "ConsultantProfiles"("Id") ON DELETE RESTRICT,
    "Status" INT NOT NULL DEFAULT 0,
        -- 0 = Scheduled, 1 = Completed, 2 = Cancelled
    "Notes" TEXT NULL,
    "BookedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CancelledAt" TIMESTAMPTZ NULL,
    "CancellationReason" TEXT NULL
);

CREATE INDEX "IX_Consultations_CustomerUserId"
    ON "Consultations"("CustomerUserId");
CREATE INDEX "IX_Consultations_ConsultantProfileId"
    ON "Consultations"("ConsultantProfileId");
CREATE INDEX "IX_Consultations_Status"
    ON "Consultations"("Status");

-- Reviews
CREATE TABLE "Reviews" (
    "Id" SERIAL PRIMARY KEY,
    "ConsultationId" INT NOT NULL UNIQUE
        REFERENCES "Consultations"("Id") ON DELETE CASCADE,
    "CustomerUserId" UUID NOT NULL
        REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "ConsultantProfileId" INT NOT NULL
        REFERENCES "ConsultantProfiles"("Id") ON DELETE CASCADE,
    "Rating" INT NOT NULL CHECK ("Rating" >= 1 AND "Rating" <= 5),
    "Comment" TEXT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_Reviews_ConsultantProfileId"
    ON "Reviews"("ConsultantProfileId");
```

---

## 11. Concurrency Pattern (Double-Booking Prevention)

```csharp
public async Task<Consultation> BookSlotAsync(int timeSlotId, Guid customerUserId, string? notes)
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

        var consultation = new Consultation
        {
            TimeSlotId = slot.Id,
            CustomerUserId = customerUserId,
            ConsultantProfileId = slot.ConsultantProfileId,
            Status = ConsultationStatus.Scheduled,
            Notes = notes,
            BookedAt = DateTime.UtcNow
        };

        _context.Consultations.Add(consultation);

        try
        {
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return consultation;
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException(
                "This slot was just booked by someone else. Please choose another.");
        }
    });
}
```

---

## 12. Seed Data

| Entity | Details |
|--------|---------|
| **Admin user** | `admin@consultify.com` / `Admin123!` / Role: Admin |
| **Consultant 1** | Sarah Chen — Career Coaching, $120/hr, 8 yrs |
| **Consultant 2** | Marcus Johnson — Business Strategy, $150/hr, 12 yrs |
| **Consultant 3** | Priya Patel — Mental Wellness, $100/hr, 6 yrs |
| **TimeSlots** | Each consultant gets 30-min slots for the next 7 days (e.g., 9:00-12:00 each day → 6 slots/day) |
| **Consultations** | 2–3 sample bookings (mix of Scheduled and Completed) |
| **Reviews** | 1–2 reviews on completed consultations |
| **Customer user** | `customer@test.com` / `Customer123!` / Role: Customer |

---

## 13. NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity with EF Core |
| `Microsoft.AspNetCore.Identity.UI` | Identity UI scaffolding |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider |
| `Microsoft.EntityFrameworkCore.Tools` | CLI migrations |
| `Microsoft.EntityFrameworkCore.Design` | Migration design-time support |

---

## 14. Implementation Order

| Phase | Description |
|-------|-------------|
| **1. Scaffold** | `dotnet new mvc` with Individual Auth, add NuGet packages, configure PostgreSQL in `appsettings.json` and `Program.cs` |
| **2. Identity Setup** | Extend `IdentityUser<Guid>` → `ApplicationUser`, configure `AppDbContext` with Identity, seed roles + admin user, custom registration with role picker |
| **3. Domain Models** | Create `ConsultantProfile`, `TimeSlot` (with RowVersion), `Consultation`, `Review` entities with FK relationships and EF configurations |
| **4. Migrations** | `dotnet ef migrations add InitialCreate` → `dotnet ef database update` |
| **5. Services Layer** | Implement `IConsultantService`, `ITimeSlotService`, `IBookingService`, `IReviewService`, `IAdminService` |
| **6. Consultant Area** | Scaffold area, implement availability (batch create 30-min slots), profile editing, consultation list + mark completed |
| **7. Customer Area** | Browse consultants, view profiles + ratings + available slots, book appointment with concurrency, cancel within 24h window |
| **8. Reviews** | Submit review on completed consultations, display ratings on consultant cards and detail pages |
| **9. Admin Area** | Dashboard with stats, user management (list, search, toggle active), consultation overview, review moderation |
| **10. UI Polish** | Bootstrap responsive layout, validation messages, error handling (404, 500), TempData flash messages, navigation highlighting |
| **11. Tests** | Unit tests for services, integration tests for booking flow, concurrency test |
