# Consultify — ASP.NET Core MVC App Plan

## Overview
A consultation booking platform with three roles: **Admin**, **Consultant**, and **Customer**. Consultants set available 30-min time slots, customers browse and book consultations.

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

## Entity Model & Relationships

```
AspNetUsers (IdentityUser extended)
├── FullName, CreatedAt
├── Roles: Admin | Consultant | Customer

Consultants (1:1 with User)
├── UserId (FK), Bio, Specialization, YearsOfExperience, HourlyRate, IsAvailable

Customers (1:1 with User)
├── UserId (FK), Phone

TimeSlots
├── ConsultantId (FK), StartTime, EndTime, IsBooked (default false)

Consultations
├── CustomerId (FK), ConsultantId (FK), TimeSlotId (FK)
├── Status: Scheduled | Completed | Cancelled
├── Notes, CreatedAt, UpdatedAt

Reviews
├── ConsultationId (FK, unique), Rating (1-5), Comment
```

### Key Relationships
- Consultant `1 — *` TimeSlots
- Customer `* — *` Consultant through Consultations
- Consultation `1 — 1` TimeSlot (slot becomes booked)
- Consultation `1 — 0..1` Review

---

## Slot Creation Flow (Auto-split, 30-min)

1. Consultant picks **Date** + **Start Time** (e.g., 9:00 AM) + **End Time** (e.g., 12:00 PM)
2. System generates individual 30-min slots: `9:00-9:30`, `9:30-10:00`, ..., `11:30-12:00`
3. Each slot saved as a separate `TimeSlot` record with `IsBooked = false`
4. Overlapping or past slots are skipped with a warning

## Booking Flow

1. Customer browses consultants → views consultant profile
2. Profile page shows a calendar/date picker with available slots per date
3. Customer clicks a slot → confirmation page → `TimeSlot.IsBooked = true` + `Consultation` created with `Status = Scheduled`
4. Consultant sees the booking on their dashboard

---

## Controllers & Actions

| Controller | Auth | Key Actions |
|---|---|---|
| `HomeController` | Public | Index, About, Contact |
| `AccountController` | Anonymous | Register (with role picker), Login, Logout, ForgotPassword |
| `ConsultantsController` | Public + Customer | Browse (list all), Details/{id} (profile + slots) |
| `AdminController` | Admin | Dashboard, Users, UserDetails/{id}, Consultations, Reviews |
| `ConsultantPanelController` | Consultant | Dashboard, Profile, EditProfile, TimeSlots, CreateSlot, DeleteSlot, MyConsultations, MarkCompleted/{id} |
| `CustomerPanelController` | Customer | Dashboard, Book/{consultantId} (pick slot), MyConsultations, ConsultationDetails/{id}, LeaveReview/{consultationId} |

---

## UI Page Map

| URL | Role | Page |
|---|---|---|
| `/` | Public | Landing page (hero, how it works, featured consultants) |
| `/Account/Register` | Public | Register with role picker (Consultant / Customer) |
| `/Account/Login` | Public | Login |
| `/Consultants` | Public | Browse all consultants (search/filter) |
| `/Consultants/{id}` | Public | Consultant profile + available slots calendar |
| `/Admin/Dashboard` | Admin | Stats: users, consultations, revenue |
| `/Admin/Users` | Admin | CRUD all users |
| `/Admin/Consultations` | Admin | All consultations, filter by status |
| `/Consultant/Dashboard` | Consultant | Upcoming consultations, quick stats |
| `/Consultant/Profile` | Consultant | Edit bio, specialization, rate |
| `/Consultant/Slots` | Consultant | Manage slots (create batch, view, delete) |
| `/Consultant/Consultations` | Consultant | View bookings, mark completed |
| `/Customer/Dashboard` | Customer | My consultations (upcoming + past) |
| `/Customer/Book/{consultantId}` | Customer | Pick date → see available slots → confirm |
| `/Customer/Review/{consultationId}` | Customer | Leave rating (1-5) + comment |

---

## Project Folder Structure

```
Consultify/
├── Models/
│   ├── ApplicationUser.cs
│   ├── Consultant.cs
│   ├── Customer.cs
│   ├── TimeSlot.cs
│   ├── Consultation.cs
│   ├── Review.cs
│   └── ViewModels/
│       ├── RegisterVM.cs
│       ├── LoginVM.cs
│       ├── ConsultantProfileVM.cs
│       ├── CreateTimeSlotVM.cs
│       ├── BookConsultationVM.cs
│       └── LeaveReviewVM.cs
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── Controllers/
│   ├── HomeController.cs
│   ├── AccountController.cs
│   ├── ConsultantsController.cs
│   ├── AdminController.cs
│   ├── ConsultantPanelController.cs
│   └── CustomerPanelController.cs
├── Views/
│   ├── Home/
│   ├── Account/
│   ├── Consultants/
│   ├── Admin/
│   ├── ConsultantPanel/
│   ├── CustomerPanel/
│   └── Shared/
├── Services/
│   ├── IConsultantService.cs / ConsultantService.cs
│   ├── ITimeSlotService.cs / TimeSlotService.cs
│   ├── IConsultationService.cs / ConsultationService.cs
│   └── IAdminService.cs / AdminService.cs
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── lib/
├── Program.cs
├── appsettings.json
└── Consultify.csproj
```

---

## NuGet Packages

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Identity.UI`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Tools`

---

## Implementation Order

1. **Scaffold** — Create project, install packages, configure PostgreSQL + Identity + seed roles
2. **Models & DbContext** — All entities + relationships + initial migration
3. **Services Layer** — Business logic (slot creation, booking, validation)
4. **AccountController** — Registration with role picker + login/logout
5. **HomeController + ConsultantsController** — Public pages (landing, browse, profile)
6. **ConsultantPanelController** — Profile editing, slot management, consultation overview
7. **CustomerPanelController** — Booking flow, consultation list, reviews
8. **AdminController** — Dashboard stats, user management, full oversight
9. **Views & UI Polish** — Razor pages, layout, navbar, responsive Bootstrap styling
10. **Seed Data** — Default admin account, sample consultants with slots, sample customer
