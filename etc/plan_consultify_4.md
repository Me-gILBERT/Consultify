# Consultify — Merged Architecture Plan

## Tech Stack
- **Framework:** ASP.NET Core 8 MVC with Razor Views
- **Database:** PostgreSQL via Entity Framework Core + Npgsql
- **Auth:** ASP.NET Core Identity with Roles (cookie/session-based)
- **ORM:** Entity Framework Core (code-first)
- **UI:** Bootstrap 5 + Data Annotations validation
- **API:** REST JSON endpoints alongside MVC (dual-mode)

---

## 1. Solution Structure

```
Consultify/
├── Consultify.sln
└── src/
    └── Consultify.Web/
        ├── Areas/
        │   ├── Admin/                          # /Admin/*
        │   │   ├── Controllers/
        │   │   │   ├── DashboardController.cs
        │   │   │   ├── UsersController.cs
        │   │   │   ├── ConsultantsController.cs
        │   │   │   ├── AppointmentsController.cs
        │   │   │   └── ReviewsController.cs
        │   │   ├── Views/
        │   │   │   ├── Dashboard/
        │   │   │   ├── Users/
        │   │   │   ├── Consultants/
        │   │   │   ├── Appointments/
        │   │   │   └── Reviews/
        │   │   └── ViewModels/
        │   ├── Consultant/                     # /Consultant/*
        │   │   ├── Controllers/
        │   │   │   ├── DashboardController.cs
        │   │   │   ├── AvailabilityController.cs
        │   │   │   ├── AppointmentsController.cs
        │   │   │   └── ProfileController.cs
        │   │   ├── Views/
        │   │   │   ├── Dashboard/
        │   │   │   ├── Availability/
        │   │   │   ├── Appointments/
        │   │   │   └── Profile/
        │   │   └── ViewModels/
        │   └── Customer/                       # /Customer/*
        │       ├── Controllers/
        │       │   ├── DashboardController.cs
        │       │   ├── ConsultantsController.cs
        │       │   ├── BookingController.cs
        │       │   ├── AppointmentsController.cs
        │       │   └── ReviewsController.cs
        │       ├── Views/
        │       │   ├── Dashboard/
        │       │   ├── Consultants/
        │       │   ├── Booking/
        │       │   ├── Appointments/
        │       │   └── Reviews/
        │       └── ViewModels/
        ├── Controllers/                        # Shared (no area)
        │   ├── HomeController.cs
        │   └── AccountController.cs
        ├── Api/                                # REST API controllers
        │   ├── Controllers/
        │   │   ├── ConsultantsApiController.cs
        │   │   ├── TimeSlotsApiController.cs
        │   │   ├── AppointmentsApiController.cs
        │   │   ├── ReviewsApiController.cs
        │   │   └── AdminApiController.cs
        │   └── ViewModels/                     # API-specific DTOs
        ├── Models/                             # Domain entities
        │   ├── ApplicationUser.cs
        │   ├── ConsultantProfile.cs
        │   ├── TimeSlot.cs
        │   ├── Appointment.cs
        │   └── Review.cs
        ├── ViewModels/                         # Shared ViewModels
        │   ├── Account/
        │   │   ├── RegisterViewModel.cs
        │   │   └── LoginViewModel.cs
        │   ├── Shared/
        │   │   ├── ConsultantCardViewModel.cs
        │   │   └── PaginationViewModel.cs
        │   └── Home/
        │       └── LandingViewModel.cs
        ├── Services/                           # Business logic
        │   ├── Interfaces/
        │   │   ├── IBookingService.cs
        │   │   ├── IConsultantService.cs
        │   │   ├── IReviewService.cs
        │   │   ├── IAdminService.cs
        │   │   └── IDashboardService.cs
        │   └── Implementations/
        │       ├── BookingService.cs
        │       ├── ConsultantService.cs
        │       ├── ReviewService.cs
        │       ├── AdminService.cs
        │       └── DashboardService.cs
        ├── Data/
        │   ├── AppDbContext.cs
        │   ├── Configurations/                 # IEntityTypeConfiguration<T>
        │   ├── SeedData.cs
        │   └── Migrations/
        ├── wwwroot/
        │   ├── css/
        │   ├── js/
        │   └── lib/
        ├── Program.cs
        ├── appsettings.json
        └── appsettings.Development.json
├── tests/
│   └── Consultify.Web.Tests/
│       ├── Services/
│       ├── Controllers/
│       └── Api/
└── Consultify.sln
```

---

## 2. Domain Models

### ApplicationUser (extended IdentityUser, GUID PK)
| Property | Type | Notes |
|----------|------|-------|
| `FirstName` | string | Required |
| `LastName` | string | Required |
| `ProfilePictureUrl` | string? | Nullable |
| `IsActive` | bool | Default true; inactive users blocked at login |
| `CreatedAt` | DateTime (UTC) | Auto-set on creation |

### Roles (seeded via ASP.NET Core Identity)
- `Admin` — created only via seeding
- `Consultant` — selected on registration
- `Customer` — selected on registration

### ConsultantProfile
| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| UserId | Guid (FK) | → ApplicationUser.Id, **unique** |
| Bio | string? | Free text |
| Specialization | string? | e.g. "Career Coaching", "Mental Health" |
| HourlyRate | decimal(18,2)? | |
| YearsOfExperience | int? | |
| IsActive | bool | Default true |
| CreatedAt | DateTime (UTC) | |

### TimeSlot
| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| ConsultantProfileId | int (FK) | → ConsultantProfile.Id |
| StartTime | DateTime (UTC) | |
| EndTime | DateTime (UTC) | Must be > StartTime |
| IsBooked | bool | Default false |
| RowVersion | byte[] | **Concurrency token** — prevents double-booking |

### Appointment
| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| TimeSlotId | int (FK) | → TimeSlot.Id, **unique** |
| CustomerUserId | Guid (FK) | → ApplicationUser.Id |
| ConsultantProfileId | int (FK) | → ConsultantProfile.Id |
| Status | AppointmentStatus | Enum: Scheduled, Completed, Cancelled, NoShow |
| BookedAt | DateTime (UTC) | |
| CancelledAt | DateTime (UTC)? | |
| CancellationReason | string? | |
| Notes | string? | Customer note at booking time |

### Review
| Property | Type | Notes |
|----------|------|-------|
| Id | int (PK) | Auto-increment |
| AppointmentId | int (FK) | → Appointment.Id, **unique** (one review per appointment) |
| CustomerUserId | Guid (FK) | → ApplicationUser.Id |
| ConsultantProfileId | int (FK) | → ConsultantProfile.Id |
| Rating | int | 1–5, server-validated |
| Comment | string? | |
| CreatedAt | DateTime (UTC) | |

---

## 3. Entity Relationships

```
ApplicationUser (1) ── (0..1) ConsultantProfile
ApplicationUser (1) ── (*) Appointment (as Customer)
ConsultantProfile (1) ── (*) TimeSlot
ConsultantProfile (1) ── (*) Appointment (as Consultant)
ConsultantProfile (1) ── (*) Review
TimeSlot (1) ── (1) Appointment
Appointment (1) ── (0..1) Review
```

---

## 4. Areas & Route Map

### Shared (No Area)

| Route | View | Auth | Description |
|-------|------|------|-------------|
| `/` | Home/Index | Anonymous | Landing page: hero, how it works, featured consultants |
| `/Home/Privacy` | Home/Privacy | Anonymous | Privacy policy |
| `/Home/Error` | Home/Error | Anonymous | Error page |
| `/Account/Register` | Account/Register | Anonymous | Register as Consultant or Customer |
| `/Account/Login` | Account/Login | Anonymous | Sign in |
| `/Account/Logout` | — | Authenticated | Sign out |
| `/Account/AccessDenied` | Account/AccessDenied | Authenticated | Role-based access denied |

### Admin Area (`/Admin/*`) — Role: Admin

| Route | View | Description |
|-------|------|-------------|
| `/Admin/Dashboard` | Dashboard | System stats: users, consultants, appointments, reviews |
| `/Admin/Users` | Users/Index | List/search users, filter by role, paginated |
| `/Admin/Users/Details/{id}` | Users/Details | View user info + their appointments |
| `/Admin/Users/ToggleStatus/{id}` | — | Enable/disable user (POST) |
| `/Admin/Consultants` | Consultants/Index | All consultants, status, profile links |
| `/Admin/Appointments` | Appointments/Index | All appointments, filter by status/date/consultant |
| `/Admin/Appointments/Details/{id}` | Appointments/Details | Full appointment info |
| `/Admin/Appointments/Cancel/{id}` | — | Force-cancel any appointment (POST) |
| `/Admin/Reviews` | Reviews/Index | All reviews, delete inappropriate ones |

### Consultant Area (`/Consultant/*`) — Role: Consultant

| Route | View | Description |
|-------|------|-------------|
| `/Consultant/Dashboard` | Dashboard | Upcoming appointments, quick stats (today/week count) |
| `/Consultant/Availability` | Availability/Index | Weekly grid of all time slots with booked status |
| `/Consultant/Availability/Create` | Availability/Create | **Auto-split form:** pick date + start time + end time → system generates 30-min slots |
| `/Consultant/Availability/Delete/{id}` | — | Delete slot (only if unbooked, POST) |
| `/Consultant/Appointments` | Appointments/Index | All my appointments, filter by status |
| `/Consultant/Appointments/Details/{id}` | Appointments/Details | View appointment |
| `/Consultant/Appointments/MarkCompleted/{id}` | — | Mark appointment completed (POST) |
| `/Consultant/Profile` | Profile/Index | View current profile |
| `/Consultant/Profile/Edit` | Profile/Edit | Edit bio, specialization, rate, experience |

### Customer Area (`/Customer/*`) — Role: Customer

| Route | View | Description |
|-------|------|-------------|
| `/Customer/Dashboard` | Dashboard | Upcoming + past appointments summary |
| `/Customer/Consultants` | Consultants/Index | Browse/search/filter all consultants |
| `/Customer/Consultants/Details/{id}` | Consultants/Details | Profile + ratings + reviews |
| `/Customer/Booking/SelectSlot/{consultantId}` | Booking/SelectSlot | Calendar of available (unbooked) slots grouped by date |
| `/Customer/Booking/Confirm/{slotId}` | Booking/Confirm | Confirm slot + add notes (GET + POST) |
| `/Customer/Appointments` | Appointments/Index | My appointments, filter upcoming/past |
| `/Customer/Appointments/Details/{id}` | Appointments/Details | View info, cancel if eligible |
| `/Customer/Appointments/Cancel/{id}` | Appointments/Cancel | Confirm + reason (GET + POST) |
| `/Customer/Reviews/Create/{appointmentId}` | Reviews/Create | Leave 1–5 rating + comment (GET + POST) |

---

## 5. REST API Endpoints (under `/api`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/consultants` | Public | List consultants (search, filter, paginate) |
| GET | `/api/consultants/{id}` | Public | Consultant profile + average rating |
| GET | `/api/consultants/{id}/slots` | Public | Available (unbooked) time slots |
| GET | `/api/consultants/{id}/reviews` | Public | Reviews for a consultant |
| POST | `/api/timeslots` | Consultant | Create a time slot (auto-split optional via query param) |
| DELETE | `/api/timeslots/{id}` | Consultant | Delete own slot (only if unbooked) |
| GET | `/api/appointments?role={role}&status={status}` | Authenticated | Get appointments filtered by role/status |
| POST | `/api/appointments` | Customer | Book a slot (body: { timeSlotId, notes }) |
| PUT | `/api/appointments/{id}/cancel` | Customer | Cancel own appointment (body: { reason }) |
| POST | `/api/reviews` | Customer | Submit review (body: { appointmentId, rating, comment }) |
| GET | `/api/admin/users` | Admin | List all users |
| PUT | `/api/admin/users/{id}/toggle-status` | Admin | Activate/deactivate user |
| DELETE | `/api/admin/reviews/{id}` | Admin | Delete a review |

---

## 6. Auto-Split Slot Generation (from Plan 1)

When a consultant creates availability, they pick a **date + start time + end time** (e.g., Mon 9:00 AM → 12:00 PM). The system automatically generates individual 30-minute slots:

```
Input:  2025-06-10, 09:00 – 12:00
Output: 09:00-09:30, 09:30-10:00, 10:00-10:30, 10:30-11:00, 11:00-11:30, 11:30-12:00
```

**Rules:**
- Overlapping existing slots are skipped (not duplicated) with a warning
- Past dates/times are rejected
- Minimum gap: start must be before end
- Rounding: start/end times are floored/ceiled to nearest 30-min boundary

---

## 7. ViewModels

### Account
- **`RegisterViewModel`** — Email, Password, ConfirmPassword, FirstName, LastName, Role (dropdown: Consultant | Customer)
- **`LoginViewModel`** — Email, Password, RememberMe

### Home
- **`LandingViewModel`** — FeaturedConsultants (list of ConsultantCardViewModel)

### Shared
- **`ConsultantCardViewModel`** — Id, FullName, Specialization, HourlyRate, AverageRating, ReviewCount, ProfilePictureUrl
- **`PaginationViewModel`** — Page, PageSize, TotalCount, TotalPages

### Admin Area
- **`DashboardStatsViewModel`** — TotalUsers, TotalConsultants, TotalCustomers, TotalAppointments, AppointmentsByStatus (dictionary), RecentAppointments (list)
- **`UserListViewModel`** — Users (paginated), RoleFilter, SearchTerm
- **`UserDetailViewModel`** — User info, role, IsActive, registration date, appointment count
- **`AppointmentListViewModel`** — Appointments (paginated), StatusFilter, DateRange
- **`ReviewListViewModel`** — Reviews (paginated), search, delete action

### Consultant Area
- **`ConsultantDashboardViewModel`** — UpcomingAppointments (next 5), TodayCount, WeeklyCount, TotalAppointments, AverageRating
- **`AvailabilityIndexViewModel`** — Slots grouped by date with booking status indicators
- **`CreateTimeSlotViewModel`** — Date, StartTime, EndTime; server generates 30-min slots
- **`ConsultantProfileViewModel`** — Bio, Specialization, HourlyRate, YearsOfExperience, AverageRating, ReviewCount

### Customer Area
- **`BrowseConsultantsViewModel`** — Consultants (paginated), SearchTerm, SpecializationFilter, SortBy
- **`ConsultantDetailViewModel`** — ConsultantProfileViewModel + AvailableSlots (grouped by date, next 14 days) + Reviews
- **`SelectSlotViewModel`** — ConsultantName, SlotsByDate (dictionary of date → list of slots)
- **`BookConfirmViewModel`** — ConsultantName, Date, StartTime–EndTime, Notes field
- **`CustomerAppointmentListViewModel`** — Appointments (paginated), UpcomingPast filter
- **`AppointmentDetailViewModel`** — Full appointment info, cancellation eligibility (within 24h window), review button if completed
- **`CancelAppointmentViewModel`** — Appointment info, CancellationReason
- **`ReviewFormViewModel`** — Appointment info, Rating (1–5), Comment

### API DTOs
- **`ApiResponse<T>`** — Success (bool), Data (T), Message (string?), Errors (list)
- **`SlotRequestDto`** — ConsultantProfileId, StartTime, EndTime, AutoSplit (bool)
- **`SlotResponseDto`** — Id, StartTime, EndTime, IsBooked
- **`BookingRequestDto`** — TimeSlotId, Notes
- **`AppointmentResponseDto`** — Id, ConsultantName, CustomerName, StartTime, EndTime, Status, Notes
- **`ReviewRequestDto`** — AppointmentId, Rating, Comment
- **`UserResponseDto`** — Id, Email, FirstName, LastName, Role, IsActive, CreatedAt

---

## 8. Services Layer

### IBookingService
```csharp
Task<List<TimeSlot>> GetAvailableSlotsAsync(int consultantProfileId);
Task<Appointment> BookSlotAsync(int timeSlotId, Guid customerUserId, string? notes);
Task<Appointment?> CancelAppointmentAsync(int appointmentId, Guid userId, string? reason);
bool CanCancel(Appointment appointment);  // 24-hour window check
```

### IConsultantService
```csharp
Task<ConsultantProfile?> GetProfileByUserIdAsync(Guid userId);
Task<ConsultantProfile?> GetProfileByIdAsync(int profileId);
Task UpdateProfileAsync(ConsultantProfile profile);
Task<List<TimeSlot>> GetTimeSlotsAsync(int consultantProfileId);
Task<List<TimeSlot>> CreateTimeSlotsAsync(int consultantProfileId, DateTime date, TimeSpan start, TimeSpan end);
    // auto-splits into 30-min slots, skips overlaps
Task<List<TimeSlot>> CreateBulkTimeSlotsAsync(int consultantProfileId, DateOnly from, DateOnly to, TimeSpan start, TimeSpan end);
    // auto-splits across multiple days, skips weekends option
Task<bool> RemoveTimeSlotAsync(int slotId, int consultantProfileId);
Task<List<Appointment>> GetAppointmentsAsync(int consultantProfileId, AppointmentStatus? status);
Task<bool> MarkCompletedAsync(int appointmentId, int consultantProfileId);
```

### IReviewService
```csharp
Task<List<Review>> GetReviewsForConsultantAsync(int consultantProfileId, int page, int pageSize);
Task<double> GetAverageRatingAsync(int consultantProfileId);
Task<int> GetReviewCountAsync(int consultantProfileId);
Task<Review> SubmitReviewAsync(int appointmentId, Guid customerUserId, int rating, string? comment);
Task<bool> DeleteReviewAsync(int reviewId);
```

### IAdminService
```csharp
Task<DashboardStatsViewModel> GetDashboardStatsAsync();
Task<PaginatedResult<ApplicationUser>> GetUsersAsync(string? role, string? search, int page, int pageSize);
Task<ApplicationUser?> GetUserByIdAsync(Guid id);
Task<bool> ToggleUserStatusAsync(Guid userId);
Task<List<Appointment>> GetAllAppointmentsAsync(AppointmentStatus? statusFilter);
Task<bool> CancelAppointmentAsync(int appointmentId, string reason);
Task<List<Review>> GetAllReviewsAsync(int page, int pageSize);
Task<bool> DeleteReviewAsync(int reviewId);
```

### IDashboardService
```csharp
Task<DashboardStatsViewModel> GetAdminStatsAsync();
Task<ConsultantDashboardViewModel> GetConsultantStatsAsync(Guid userId);
```

---

## 9. Concurrency Pattern (Double-Booking Prevention)

Uses EF Core's `RowVersion` concurrency token on `TimeSlot`:

```csharp
// In BookingService.BookSlotAsync
public async Task<Appointment> BookSlotAsync(int timeSlotId, Guid customerUserId, string? notes)
{
    using var strategy = _context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        var slot = await _context.TimeSlots
            .FirstOrDefaultAsync(s => s.Id == timeSlotId);

        if (slot == null || slot.IsBooked)
            throw new InvalidOperationException("This slot is no longer available.");

        slot.IsBooked = true;

        var appointment = new Appointment
        {
            TimeSlotId = slot.Id,
            CustomerUserId = customerUserId,
            ConsultantProfileId = slot.ConsultantProfileId,
            Status = AppointmentStatus.Scheduled,
            BookedAt = DateTime.UtcNow,
            Notes = notes
        };

        _context.Appointments.Add(appointment);

        try
        {
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return appointment;
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

## 10. Key Business Rules

1. **Double-booking prevention** — `RowVersion` concurrency token on TimeSlot; simultaneous booking attempts result in one succeeding and the others receiving a clear error.
2. **Slot deletion** — Consultant can only delete a TimeSlot where `IsBooked == false`.
3. **Cancellation window** — Customer can cancel up to **24 hours before** `StartTime`. After that, only the consultant or admin can cancel.
4. **Reviews** — Only on appointments with `Status == Completed`. One review per appointment (unique constraint on `AppointmentId`).
5. **Rating range** — 1–5 inclusive, validated server-side.
6. **Profile deactivation** — Admin can deactivate any user. Deactivated users see an error on login (checked via custom `SignInManager` or claims factory).
7. **Registration** — New users pick Consultant or Customer. Admin accounts are seeded only.
8. **Slot auto-split** — Overlapping existing slots are skipped, not duplicated.
9. **Data ownership** — Consultants see only their own slots/appointments. Customers see only their own bookings.

---

## 11. Seed Data

| Entity | Details |
|--------|---------|
| **Admin** | `admin@consultify.com` / `Admin123!` — Role: Admin, Name: System Admin |
| **Consultant 1** | `sarah.chen@consultify.com` / `Consult123!` — Career Coaching, $120/hr, 8 yrs |
| **Consultant 2** | `marcus.johnson@consultify.com` / `Consult123!` — Business Strategy, $150/hr, 12 yrs |
| **Consultant 3** | `priya.patel@consultify.com` / `Consult123!` — Mental Wellness, $100/hr, 6 yrs |
| **Customer 1** | `alice@example.com` / `Customer123!` |
| **Customer 2** | `bob@example.com` / `Customer123!` |
| **TimeSlots** | Each consultant gets 30-min slots for the next 7 days (auto-generated, 4h/day each) |
| **Appointments** | 3 sample: 2 Scheduled (upcoming), 1 Completed (past) |
| **Reviews** | 1 review on the completed appointment |

---

## 12. NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity + EF Core integration |
| `Microsoft.AspNetCore.Identity.UI` | Identity UI scaffold |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL EF Core provider |
| `Microsoft.EntityFrameworkCore.Tools` | CLI migration commands |
| `Microsoft.EntityFrameworkCore.Design` | Migration design-time support |

---

## 13. Database Schema (SQL)

```sql
-- AspNetUsers (extended Identity)
CREATE TABLE "AspNetUsers" (
    "Id" UUID PRIMARY KEY,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "ProfilePictureUrl" TEXT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    -- inherited Identity columns: UserName, Email, PasswordHash, etc.
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
    "RowVersion" BYTEA NOT NULL DEFAULT '\\x0000000000000000'
);
CREATE INDEX "IX_TimeSlots_ConsultantProfileId" ON "TimeSlots"("ConsultantProfileId");
CREATE INDEX "IX_TimeSlots_StartTime" ON "TimeSlots"("StartTime");
CREATE INDEX "IX_TimeSlots_IsBooked" ON "TimeSlots"("IsBooked");

-- Appointments
CREATE TABLE "Appointments" (
    "Id" SERIAL PRIMARY KEY,
    "TimeSlotId" INT NOT NULL UNIQUE
        REFERENCES "TimeSlots"("Id") ON DELETE RESTRICT,
    "CustomerUserId" UUID NOT NULL
        REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "ConsultantProfileId" INT NOT NULL
        REFERENCES "ConsultantProfiles"("Id") ON DELETE RESTRICT,
    "Status" INT NOT NULL DEFAULT 0,
        -- 0=Scheduled, 1=Completed, 2=Cancelled, 3=NoShow
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
    "AppointmentId" INT NOT NULL UNIQUE
        REFERENCES "Appointments"("Id") ON DELETE CASCADE,
    "CustomerUserId" UUID NOT NULL
        REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT,
    "ConsultantProfileId" INT NOT NULL
        REFERENCES "ConsultantProfiles"("Id") ON DELETE CASCADE,
    "Rating" INT NOT NULL CHECK ("Rating" >= 1 AND "Rating" <= 5),
    "Comment" TEXT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX "IX_Reviews_ConsultantProfileId" ON "Reviews"("ConsultantProfileId");
```

---

## 14. Implementation Order

| Phase | Tasks |
|-------|-------|
| **1. Scaffold** | `dotnet new mvc` with Individual Auth, add NuGet packages, configure PostgreSQL in `Program.cs` and `appsettings.json` |
| **2. Identity Setup** | Extend `ApplicationUser`, configure `AppDbContext`, seed roles + admin + sample users, custom registration with role picker |
| **3. Domain Models** | Create `ConsultantProfile`, `TimeSlot` (with RowVersion), `Appointment`, `Review` entities + EF Configurations |
| **4. Migrations** | `dotnet ef migrations add InitialCreate` → `dotnet ef database update` |
| **5. Services Layer** | Implement all services: `BookingService`, `ConsultantService`, `ReviewService`, `AdminService`, `DashboardService` |
| **6. Consultant Area** | Scaffold Area, implement slot CRUD with auto-split, profile editing, appointment overview |
| **7. Customer Area** | Browse consultants, view available slots (grouped by date), book flow, cancellation, reviews |
| **8. Admin Area** | Dashboard stats, user management (list/search/toggle), appointment oversight, review moderation |
| **9. REST API** | All `/api/*` endpoints with consistent `ApiResponse<T>` envelope |
| **10. Polish** | Bootstrap 5 responsive UI, validation messages, error handling (404/500), TempData flash messages, loading states |

---

## 15. Origins

This plan merges the best of three prior versions:
- **Plan 1** → Auto-split slot generation (30-min), Reviews, clear panel separation
- **Plan 2** → REST API endpoints, simplified entity model
- **Plan 3** → Areas, concurrency (RowVersion), detailed ViewModels, business rules, seed data, test structure
