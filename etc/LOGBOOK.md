# Consultify — Build Logbook

**Date:** 2026-05-19  
**Tech Stack:** ASP.NET Core 10 MVC, PostgreSQL, Entity Framework Core, ASP.NET Core Identity  
**Plan Reference:** `plan_consultify_4.md`

---

## Table of Contents

1. [Phase 1: Project Scaffolding](#phase-1-project-scaffolding)
2. [Phase 2: Identity Setup](#phase-2-identity-setup)
3. [Phase 3: Domain Models](#phase-3-domain-models)
4. [Phase 4: EF Migrations](#phase-4-ef-migrations)
5. [Phase 5: Services Layer](#phase-5-services-layer)
6. [Phase 6: Consultant Area](#phase-6-consultant-area)
7. [Phase 7: Customer Area](#phase-7-customer-area)
8. [Phase 8: Admin Area](#phase-8-admin-area)
9. [Phase 9: REST API](#phase-9-rest-api)
10. [Phase 10: Polish, Tests & Final Build](#phase-10-polish-tests--final-build)

---

## Phase 1: Project Scaffolding

### 1.1 Environment Check
- Verified .NET SDK version: `10.0.300`
- Working directory: `C:\Users\gilbe\Downloads\CONSULTIFY_Final`
- Empty directory — greenfield project

### 1.2 Solution and Project Creation
```bash
dotnet new sln -n Consultify
dotnet new mvc -n Consultify.Web -o src\Consultify.Web --auth Individual --use-local-db false
dotnet sln add src\Consultify.Web\Consultify.Web.csproj
```

**Result:**
- Created `Consultify.slnx` (new .NET 10 solution format)
- `src/Consultify.Web/` with full MVC + Identity scaffolding
- Generated files: `Program.cs`, `appsettings.json`, `Controllers/HomeController.cs`, `Views/`, `wwwroot/`, `Data/ApplicationDbContext.cs`, `Areas/Identity/`, initial SQLite migration

### 1.3 NuGet Packages Added
```bash
dotnet add src\Consultify.Web package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src\Consultify.Web package Microsoft.EntityFrameworkCore.Tools
```

**Result:** PostgreSQL provider (v10.0.1) and EF Tools added to `.csproj`

### 1.4 Template Review
Read generated files to understand baseline:
- `Program.cs` — SQLite configured, `IdentityUser` (string PK), `AddDefaultIdentity<IdentityUser>`
- `appsettings.json` — SQLite connection string
- `ApplicationDbContext.cs` — `IdentityDbContext` (no custom user)
- Migration schema — string PKs (`AspNetUsers.Id = TEXT`)

### 1.5 Decision: Switch to GUID PKs
Decided to follow the plan which specifies `ApplicationUser : IdentityUser<Guid>` with GUID foreign keys. This required:
- Removing the existing SQLite migration and `app.db`
- Switching from `IdentityUser` to custom `ApplicationUser`

---

## Phase 2: Identity Setup

### 2.1 ApplicationUser Model
Created `Models/ApplicationUser.cs`
```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // Navigation properties
    public ConsultantProfile? ConsultantProfile { get; set; }
    public ICollection<Appointment> CustomerAppointments { get; set; }
}
```

### 2.2 ApplicationDbContext Update
Updated to use `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` with full Fluent API configuration:
- Max lengths, decimal precision, indexes, cascade/restrict delete behaviors
- All entity relationships defined (see [Phase 3](#phase-3-domain-models))

### 2.3 Program.cs Configuration
- Switched from `UseSqlite` to `UseNpgsql`
- Changed `AddDefaultIdentity<IdentityUser>` to `AddDefaultIdentity<ApplicationUser>` with `.AddRoles<IdentityRole<Guid>>()`
- Disabled `RequireConfirmedAccount` for development
- Added area route registration: `{area:exists}/{controller=Dashboard}/{action=Index}/{id?}`

### 2.4 Connection String
Updated `appsettings.json` to PostgreSQL:
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=consultify;Username=postgres;Password=postgres"
```

### 2.5 CSProj Cleanup
Removed duplicate `Npgsql.EntityFrameworkCore.PostgreSQL` and leftover `Microsoft.EntityFrameworkCore.Sqlite` references. First build succeeded (0 errors, 0 warnings).

### 2.6 SeedData Class
Created `Data/SeedData.cs` with:
- Role seeding: Admin, Consultant, Customer
- Admin user: `admin@consultify.com` / `Admin123!`
- 3 consultants: Sarah Chen (Career Coaching, $120/hr), Marcus Johnson (Business Strategy, $150/hr), Priya Patel (Mental Wellness, $100/hr)
- Each consultant gets 30-min slots for 7 days (Mon-Fri, 9AM-12PM)
- 2 customers: Alice Johnson and Bob Smith
- Added seed call in `Program.cs`

### 2.7 Directory Structure
Created full folder hierarchy matching plan_consultify_4.md:
- Areas: Admin, Consultant, Customer (Controllers, Views, ViewModels)
- Api: Controllers, ViewModels
- Services: Interfaces, Implementations
- ViewModels: Account, Shared, Home
- Data: Configurations
- Tests: Consultify.Web.Tests

### 2.8 _LoginPartial Update
Updated from `IdentityUser` to `ApplicationUser` injection, changed nav link colors to match new Bootstrap primary theme.

---

## Phase 3: Domain Models

### 3.1 Entity Relationship Diagram

```
ApplicationUser (Guid PK)
    ├── (1) ── (0..1) ConsultantProfile
    └── (1) ── (*) Appointment (as Customer)

ConsultantProfile (int PK)
    ├── (1) ── (*) TimeSlot
    ├── (1) ── (*) Appointment (as Consultant)
    └── (1) ── (*) Review

TimeSlot (int PK)
    ├── RowVersion (concurrency token)
    └── (1) ── (1) Appointment

Appointment (int PK)
    └── (1) ── (0..1) Review

Review (int PK)
```

### 3.2 Models Created

**ConsultantProfile.cs**
- Fields: Id, UserId (FK, unique), Bio, Specialization, HourlyRate, YearsOfExperience, IsActive, CreatedAt
- Nav: User (1:1), TimeSlots, Appointments, Reviews

**TimeSlot.cs**
- Fields: Id, ConsultantProfileId (FK), StartTime, EndTime, IsBooked, RowVersion `[Timestamp]`
- Nav: ConsultantProfile, Appointment (1:1)

**Appointment.cs**
- Fields: Id, TimeSlotId (FK, unique), CustomerUserId (FK), ConsultantProfileId (FK), Status (enum), BookedAt, CancelledAt, CancellationReason, Notes
- Status enum: `Scheduled`, `Completed`, `Cancelled`, `NoShow`
- Nav: TimeSlot, CustomerUser, ConsultantProfile, Review

**Review.cs**
- Fields: Id, AppointmentId (FK, unique), CustomerUserId (FK), ConsultantProfileId (FK), Rating (1-5), Comment, CreatedAt
- Nav: Appointment, CustomerUser, ConsultantProfile

### 3.3 DbContext Configuration
Applied via Fluent API in `OnModelCreating`:
- Cascade deletes where appropriate (ConsultantProfile → TimeSlots, Appointment → Review)
- Restrict deletes on IdentityUser references
- Indexes on FK columns and frequently-filtered fields (Status, StartTime, IsBooked)
- Unique indexes on UserId, TimeSlotId, AppointmentId

---

## Phase 4: EF Migrations

### 4.1 Initial Migration
```bash
dotnet ef migrations add InitialCreate --project src\Consultify.Web
```

**Issue:** Migration was created in root `Migrations/` folder instead of `Data/Migrations/`
**Fix:** Moved files and removed old folder. Future commands will need `--output-dir Data/Migrations`

### 4.2 Migration Verification
- Reviewed migration contents — all Identity tables + custom entities with proper column types
- Verified RowVersion column sets as PostgreSQL `bytea` with `xmin` concurrency
- Build succeeded after migration creation

---

## Phase 5: Services Layer

### 5.1 Interfaces Created

| Interface | Key Methods |
|-----------|-------------|
| `IBookingService` | `GetAvailableSlotsAsync`, `BookSlotAsync` (with concurrency), `CancelAppointmentAsync`, `CanCancel` |
| `IConsultantService` | `GetProfileByUserId/Id`, `UpdateProfile`, `CreateTimeSlotsAsync` (auto-split 30-min), `CreateBulkTimeSlotsAsync` (multi-day), `RemoveTimeSlotAsync`, `GetAppointmentsAsync`, `MarkCompletedAsync` |
| `IReviewService` | `GetReviewsForConsultantAsync`, `GetAverageRating`, `GetReviewCount`, `SubmitReviewAsync`, `DeleteReviewAsync` |
| `IAdminService` | `GetTotalUsers/Consultants/Customers/Appointments`, `GetUsersAsync` (paginated+filtered), `GetUserById`, `ToggleUserStatus`, `GetAllAppointments`, `CancelAppointment`, `GetAllReviews`, `DeleteReview` |
| `IDashboardService` | `GetAdminStatsAsync`, `GetConsultantStatsAsync` (includes DTO classes) |

### 5.2 Implementations

**BookingService.cs** — Core booking logic:
- `GetAvailableSlotsAsync`: Returns future, unbooked slots ordered by date
- `BookSlotAsync`: Uses `CreateExecutionStrategy` + serializable transaction for double-booking prevention. Catches `DbUpdateConcurrencyException` on `RowVersion`
- `CanCancel`: Checks status == Scheduled AND start > 24h from now

**ConsultantService.cs** — Slot management:
- `CreateTimeSlotsAsync`: Takes date + start/end time, generates 30-min intervals, skips past slots and existing overlaps
- `CreateBulkTimeSlotsAsync`: Wraps single-day creation across date range, skips weekends
- `RemoveTimeSlotAsync`: Only deletes if `IsBooked == false`

**ReviewService.cs** — Validation:
- Ensures only completed appointments can be reviewed
- One review per appointment (unique constraint)
- Rating must be 1-5

**AdminService.cs** — Admin operations:
- Role-based counting via `AspNetUserRoles` table
- Paginated user list with search and role filter
- Review deletion

**DashboardService.cs** — Stats:
- Admin: total counts + appointments by status + recent 10
- Consultant: upcoming 5, today count, weekly count, average rating

### 5.3 DI Registration
All 5 services registered in `Program.cs` as `AddScoped`.

---

## Phase 6: Consultant Area

### 6.1 ViewModels
```csharp
ConsultantDashboardVM  — TodayCount, WeeklyCount, TotalAppointments, AverageRating, UpcomingAppointments
CreateTimeSlotVM       — Date, StartTime, EndTime
ConsultantProfileVM    — Bio, Specialization, HourlyRate, YearsOfExperience
```

### 6.2 Controllers

**DashboardController** (`/Consultant/Dashboard`)
- `Index()` — Calls `IDashboardService.GetConsultantStatsAsync`, maps to `ConsultantDashboardVM`

**AvailabilityController** (`/Consultant/Availability`)
- `Index()` — Lists all slots grouped by date with status badges (Booked/Past/Available)
- `Create()` (GET+POST) — Batch slot creation form with auto-split UX; success message shows count of slots created
- `Delete()` (POST) — Deletes slot (blocked server-side if booked)

**AppointmentsController** (`/Consultant/Appointments`)
- `Index(status)` — Filterable list of appointments with status badges
- `MarkCompleted(id)` (POST) — Marks scheduled appointment as completed

**ProfileController** (`/Consultant/Profile`)
- `Index()` — View current profile details
- `Edit()` (GET+POST) — Edit bio, specialization, hourly rate, experience

### 6.3 Views
6 Razor views created:
- `Dashboard/Index.cshtml` — Stat cards + upcoming appointments table
- `Availability/Index.cshtml` — Slots grouped by date cards with delete forms
- `Availability/Create.cshtml` — Form with time pickers + example sidebar
- `Appointments/Index.cshtml` — Filter dropdown + appointments table with mark-completed
- `Profile/Index.cshtml` — Read-only detail view
- `Profile/Edit.cshtml` — Edit form with validation

---

## Phase 7: Customer Area

### 7.1 ViewModels
```csharp
BrowseConsultantsVM — Consultants list, SearchTerm, SpecializationFilter, Pagination
ConsultantCardVM    — ProfileId, FullName, Specialization, HourlyRate, AverageRating, ReviewCount
ConsultantDetailVM  — Full profile + AvailableSlots grouped by date (14 days) + Reviews
BookConfirmVM       — TimeSlotId, ConsultantName, StartTime, EndTime, Notes
```

### 7.2 Controllers

**DashboardController** (`/Customer/Dashboard`)
- `Index()` — Shows upcoming (next 5) and past (last 5) appointments in split cards

**ConsultantsController** (`/Customer/Consultants`)
- `Index(search, specialization, page)` — Searchable, filterable, paginated consultant cards with ratings
- `Details(id)` — Profile view + available slots for next 14 days (grouped by date) + reviews

**BookingController** (`/Customer/Booking`)
- `Confirm(slotId)` (GET) — Shows booking confirmation with consultant name, date, time
- `Confirm(BookConfirmVM)` (POST) — Calls `BookingService.BookSlotAsync` with concurrency; redirects to appointments on success

**AppointmentsController** (`/Customer/Appointments`)
- `Index()` — All appointments with review/cancel actions
- `Cancel(id, reason)` (POST) — Customer cancellation within 24h window

**ReviewsController** (`/Customer/Reviews`)
- `Create(appointmentId)` (GET+POST) — Rating (1-5 radio) + comment form

### 7.3 Views
6 Razor views created:
- `Dashboard/Index.cshtml` — Split upcoming/past cards
- `Consultants/Index.cshtml` — Search bar, specialization filter, card grid, pagination
- `Consultants/Details.cshtml` — Profile card + reviews + available slot buttons
- `Booking/Confirm.cshtml` — Booking details + notes field
- `Appointments/Index.cshtml` — Full table with cancel/review action buttons
- `Reviews/Create.cshtml` — Star rating + comment form

---

## Phase 8: Admin Area

### 8.1 ViewModels
```csharp
DashboardStatsVM     — TotalUsers, TotalConsultants, TotalCustomers, TotalAppointments, AppointmentsByStatus, RecentAppointments
UserListVM           — Users (paginated), RoleFilter, SearchTerm
UserDetailVM         — User + Role + AppointmentCount
AppointmentListVM    — Appointments + StatusFilter
ReviewListVM         — Reviews (paginated)
```

### 8.2 Controllers

**DashboardController** (`/Admin/Dashboard`)
- `Index()` — Stats cards + appointments-by-status table + recent appointments list

**UsersController** (`/Admin/Users`)
- `Index(role, search, page)` — Searchable, filterable by role, paginated user table
- `Details(id)` — Single user info with role, status, registration date
- `ToggleStatus(id)` (POST) — Activate/deactivate toggle

**AppointmentsController** (`/Admin/Appointments`)
- `Index(status)` — All appointments filterable by status with inline cancel form
- `Cancel(id, reason)` (POST) — Force-cancel any appointment

**ReviewsController** (`/Admin/Reviews`)
- `Index(page)` — All reviews with consultant/customer info
- `Delete(id)` (POST) — Remove inappropriate reviews

### 8.3 Views
5 Razor views created:
- `Dashboard/Index.cshtml` — Stats cards + status breakdown + recent list
- `Users/Index.cshtml` — Filter form + table with activate/deactivate
- `Users/Details.cshtml` — User info display
- `Appointments/Index.cshtml` — Filter + table with cancel input
- `Reviews/Index.cshtml` — Table with delete button

---

## Phase 9: REST API

### 9.1 API Response Envelope
```csharp
ApiResponse<T> — Success (bool), Data (T), Message (string?), Errors (List<string>?)
Static helpers: Ok(data, message), Fail(message, errors)
```

### 9.2 API DTOs
- `SlotRequestDto`, `SlotResponseDto` — Time slot CRUD
- `BookingRequestDto`, `AppointmentResponseDto` — Booking flow
- `ReviewRequestDto` — Review submission
- `UserResponseDto` — User listing

### 9.3 Controllers

**ConsultantsApiController** (`GET /api/consultants`, `GET /api/consultants/{id}`, `GET /api/consultants/{id}/reviews`)
- Public endpoints for browsing consultants and reading reviews

**TimeSlotsApiController** (`GET /api/timeslots`, `POST /api/timeslots`, `DELETE /api/timeslots/{id}`)
- Consultant-authorized slot management with auto-split support

**AppointmentsApiController** (`GET /api/appointments`, `POST /api/appointments`, `PUT /api/appointments/{id}/cancel`)
- Role-filtered listing (customer vs consultant), customer-only booking, authenticated cancellation

**ReviewsApiController** (`POST /api/reviews`)
- Customer-only review submission

**AdminApiController** (`GET /api/admin/users`, `PUT /api/admin/users/{id}/toggle-status`, `DELETE /api/admin/reviews/{id}`)
- Admin-only user management and review moderation

**Bug fix:** `ThenLoad` does not exist on `ReferenceEntry` in EF Core 10. Fixed by loading full related data via separate `Include` query after booking.

---

## Phase 10: Polish, Tests & Final Build

### 10.1 Layout & Navigation
- Updated `_Layout.cshtml` with Bootstrap 5 primary navbar
- Role-based nav items: Consultant/Customer/Admin see their respective links
- TempData-based success/error alerts (Bootstrap dismissible)
- Home landing page with role-based call-to-action cards

### 10.2 _LoginPartial
- Updated to use `ApplicationUser` injection
- Styled nav links to match primary theme

### 10.3 Test Project
```bash
dotnet new xunit -n Consultify.Web.Tests
dotnet add reference src/Consultify.Web/Consultify.Web.csproj
dotnet add package Moq
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

**Test files created:**

**BookingServiceTests (3 tests):**
- `GetAvailableSlots_ReturnsOnlyUnbookedFutureSlots` — Filters out past and booked slots
- `BookSlotAsync_ThrowsWhenSlotAlreadyBooked` — Double-booking prevention
- `CanCancel_ReturnsFalseForPastAppointment` — 24h window enforcement

**ConsultantServiceTests (3 tests):**
- `CreateTimeSlotsAsync_Creates30MinSlots` — Verifies 6 slots from 9-12 range
- `CreateTimeSlotsAsync_ThrowsForPastDate` — Past date validation
- `RemoveTimeSlotAsync_OnlyRemovesUnbooked` — Protection against booked slot deletion

Plus 1 default test from template = **7 tests total — all passing.**

### 10.4 Solution Build
Final full solution build: **0 warnings, 0 errors**
```
Consultify.Web -> bin/Debug/net10.0/Consultify.Web.dll
Consultify.Web.Tests -> bin/Debug/net10.0/Consultify.Web.Tests.dll
```

---

## Final Project Structure

```
Consultify/
├── Consultify.slnx
├── plan_consultify_4.md
├── LOGBOOK.md
├── src/
│   └── Consultify.Web/
│       ├── Areas/
│       │   ├── Admin/
│       │   │   ├── Controllers/    (Dashboard, Users, Appointments, Reviews)
│       │   │   ├── Views/          (Dashboard, Users, Appointments, Reviews)
│       │   │   └── ViewModels/     (AdminViewModels.cs)
│       │   ├── Consultant/
│       │   │   ├── Controllers/    (Dashboard, Availability, Appointments, Profile)
│       │   │   ├── Views/          (Dashboard, Availability, Appointments, Profile)
│       │   │   └── ViewModels/     (ConsultantDashboardVM, CreateTimeSlotVM, ConsultantProfileVM)
│       │   └── Customer/
│       │       ├── Controllers/    (Dashboard, Consultants, Booking, Appointments, Reviews)
│       │       ├── Views/          (Dashboard, Consultants, Booking, Appointments, Reviews)
│       │       └── ViewModels/     (CustomerViewModels.cs)
│       ├── Api/
│       │   ├── Controllers/        (Consultants, TimeSlots, Appointments, Reviews, Admin)
│       │   └── ViewModels/         (ApiResponse, ApiDtos)
│       ├── Controllers/            (HomeController)
│       ├── Models/                 (ApplicationUser, ConsultantProfile, TimeSlot, Appointment, Review)
│       ├── Services/
│       │   ├── Interfaces/         (IBookingService, IConsultantService, IReviewService, IAdminService, IDashboardService)
│       │   └── Implementations/    (BookingService, ConsultantService, ReviewService, AdminService, DashboardService)
│       ├── Data/                   (AppDbContext, SeedData, Migrations)
│       ├── ViewModels/             (Account, Shared, Home)
│       └── wwwroot/                (css, js, lib)
└── tests/
    └── Consultify.Web.Tests/
        ├── Services/
        │   ├── BookingServiceTests.cs
        │   └── ConsultantServiceTests.cs
        └── Consultify.Web.Tests.csproj
```

## Phase 11: Bug Fixes — DateTime Kind & RowVersion

### 11.1 DateTime Kind=Unspecified (Create Time Slots)

**Symptoms:** "An error occurred while saving the entity changes. See the inner exception for details." when submitting the Create Time Slots form.

**Root cause:** `DateTime.Date` silently resets `DateTimeKind` to `Unspecified`. The migration creates all DateTime columns as `timestamp with time zone`. Npgsql 10.0.1 rejects `Unspecified` kind for `timestamptz` columns.

**Files changed:**
| File | Change |
|------|--------|
| `Services/Implementations/ConsultantService.cs:62,64` | Wrapped `date.Date.Add(start/end)` with `DateTime.SpecifyKind(..., DateTimeKind.Utc)` |
| `Data/SeedData.cs:82` | Replaced `DateTime.UtcNow.Date` with explicit UTC midnight constructor |
| `Program.cs:8` | Added `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` |

### 11.2 RowVersion NOT NULL Violation (Create Time Slots)

**Symptoms:** "null value in column 'RowVersion' of relation 'TimeSlots' violates not-null constraint" — revealed by adding inner exception detail in catch block.

**Root cause:** `TimeSlot.RowVersion` was `byte[]` with `[Timestamp]` attribute, creating a `bytea NOT NULL` column. EF Core's `[Timestamp]` skips the column on INSERT (expecting DB auto-generation), but PostgreSQL's `bytea` has no auto-generation → NULL violates NOT NULL.

**Fix:** Removed `RowVersion` property entirely. `BookingService` already uses `Serializable` isolation level transactions for concurrency protection, making `RowVersion` redundant.

**Files changed:**
| File | Change |
|------|--------|
| `Models/TimeSlot.cs` | Removed `RowVersion` property (was `[Timestamp] byte[]`) |
| `Data/ApplicationDbContext.cs` | Removed `.IsRowVersion()` Fluent API config |
| `Services/Implementations/BookingService.cs` | Extended catch to handle `PostgresException` serialization failures (SqlState "40001") in addition to `DbUpdateConcurrencyException` |

### 11.3 Migration Applied
```bash
dotnet ef migrations add RemoveRowVersion --output-dir Data/Migrations
dotnet ef database update
```
**Migration actions:**
1. `ALTER TABLE "TimeSlots" DROP COLUMN "RowVersion"`
2. Converted all `timestamp with time zone` columns to `timestamp without time zone` (due to `EnableLegacyTimestampBehavior` switch):
   - TimeSlots.StartTime, TimeSlots.EndTime
   - Reviews.CreatedAt
   - ConsultantProfiles.CreatedAt
   - AspNetUsers.CreatedAt
   - Appointments.BookedAt, Appointments.CancelledAt

### 11.4 Tests
All 6 tests pass after changes. Test count decreased from 7 to 6 (1 default template test removed earlier).

---

## Key Decisions & Notes

| Decision | Rationale |
|----------|-----------|
| GUID PKs instead of string | Plan 4 specifies GUID; better for distributed systems and avoids GUID-to-string conversion overhead |
| Fluent API over Data Annotations | Centralized configuration, cleaner entity classes, supports advanced EF features |
| Areas instead of flat Controllers | Proper ASP.NET Core pattern for role-separated sections; cleaner routing with `{area:exists}` |
| Separate ConsultantProfile table | Instead of adding fields to ApplicationUser; keeps Identity user clean and allows profile to be independently managed |
| InMemory database for tests | Avoids PostgreSQL dependency in CI; suitable for unit-testing service logic |
| Removed RowVersion concurrency | `BookingService` already uses `Serializable` isolation; `bytea NOT NULL` column with no auto-generation caused INSERT failures |
| `Npgsql.EnableLegacyTimestampBehavior` | Avoids strict DateTimeKind checking; all DateTimes stored as `timestamp without time zone` with app-managed UTC convention |

## Prerequisites to Run

1. PostgreSQL running on `localhost:5432` (or update connection string in `appsettings.json`)
2. Run: `dotnet ef database update -p src/Consultify.Web -o Data/Migrations`
3. Run: `dotnet run --project src/Consultify.Web`
4. Login with seeded accounts:
   - Admin: `admin@consultify.com` / `Admin123!`
   - Consultant: `sarah.chen@consultify.com` / `Consult123!`
   - Customer: `alice@example.com` / `Customer123!`
