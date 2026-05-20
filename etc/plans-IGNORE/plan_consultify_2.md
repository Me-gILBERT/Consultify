# Consultify — MVC App Plan

## Tech Stack
- **Framework:** ASP.NET Core MVC
- **Database:** PostgreSQL (via Entity Framework Core + Npgsql)
- **Authentication:** Session-based (ASP.NET Core Identity)
- **API:** REST API alongside MVC views

## Roles & Permissions

| Role | Capabilities |
|------|-------------|
| **Admin** | Manage users (activate/deactivate, change roles), view all consultations, system dashboard |
| **Consultant** | Create/manage free time slots, view their own bookings |
| **Customer** | Browse consultants, view available slots, book consultations, manage their bookings |

## Database Schema (Entity Framework Core + PostgreSQL)

```
Users (extended IdentityUser)
├── Id, Name, Email, Role (enum: Admin|Consultant|Customer), CreatedAt

TimeSlots
├── Id, ConsultantId (FK→User), StartTime, EndTime, IsBooked (bool), CreatedAt

Consultations
├── Id, CustomerId (FK→User), TimeSlotId (FK→TimeSlot), Status (Scheduled|Completed|Cancelled), Notes, BookedAt

ConsultantProfiles
├── Id, ConsultantId (FK→User), Bio, Specialization, HourlyRate
```

## MVC Folder Structure

```
Consultify/
├── Controllers/          # HomeController, AccountController, AdminController,
│                         # ConsultantController, CustomerController
├── Models/
│   ├── Entities/         # User, TimeSlot, Consultation, ConsultantProfile
│   └── ViewModels/       # LoginVM, RegisterVM, BookSlotVM, DashboardVM, etc.
├── Views/
│   ├── Account/          # Login, Register
│   ├── Admin/            # Dashboard, ManageUsers, AllConsultations
│   ├── Consultant/       # Dashboard, MySlots, CreateSlot, MyBookings
│   ├── Customer/         # Dashboard, BrowseConsultants, ConsultantDetail, MyConsultations
│   └── Shared/           # _Layout, _Validation
├── Api/
│   └── Controllers/      # TimeSlotsApi, ConsultationsApi, UsersApi, AdminApi
├── Services/             # ITimeSlotService, IConsultationService, IUserService + implementations
├── Data/                 # AppDbContext, Migrations, SeedData
├── wwwroot/              # css/, js/ (Bootstrap + custom)
├── Program.cs            # DI setup, auth config, middleware
└── appsettings.json
```

## Core Workflows

### Consultant Flow
```
Login → Dashboard → "Create Free Slot" → Pick date/time range
                  → "My Slots" → See all slots with booked/available status
                  → "My Consultations" → View upcoming booked sessions
```

### Customer Flow
```
Login → Dashboard → "Browse Consultants" → List of consultants
       → Click consultant → See their available slots → Pick a slot
       → Consultation booked automatically → "My Consultations" to view
```

### Admin Flow
```
Login → Dashboard (stats: total users, consultations, etc.)
       → "Manage Users" → View/search, deactivate, change roles
       → "All Consultations" → View system-wide, cancel if needed
```

## Key Routes

### MVC Routes
| Route | Purpose |
|-------|---------|
| `GET/POST /Account/Login` | Login |
| `GET/POST /Account/Register` | Register with role selection |
| `POST /Account/Logout` | Logout |
| `GET /Admin/Dashboard` | Admin dashboard |
| `GET /Admin/ManageUsers` | Manage all users |
| `GET /Admin/ManageConsultations` | View all consultations |
| `GET /Consultant/Dashboard` | Consultant dashboard |
| `GET/POST /Consultant/CreateTimeSlot` | Create available time slot |
| `GET /Consultant/MyTimeSlots` | View own time slots |
| `GET /Consultant/MyConsultations` | View booked consultations |
| `GET /Customer/Dashboard` | Customer dashboard |
| `GET /Customer/BrowseConsultants` | Browse consultants |
| `GET /Customer/ConsultantDetail/{id}` | View consultant + available slots |
| `POST /Customer/BookSlot/{timeSlotId}` | Book a time slot |
| `GET /Customer/MyConsultations` | View own consultations |

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/timeslots?consultantId={id}` | Get available slots for a consultant |
| POST | `/api/timeslots` | Create a time slot (consultant) |
| DELETE | `/api/timeslots/{id}` | Delete a time slot (consultant) |
| GET | `/api/consultations?userId={id}&role={role}` | Get consultations for a user |
| POST | `/api/consultations` | Book a consultation (customer) |
| PUT | `/api/consultations/{id}/cancel` | Cancel a consultation |
| GET | `/api/users/consultants` | List all consultants |
| GET | `/api/admin/users` | List all users (admin only) |
| PUT | `/api/admin/users/{id}/role` | Change user role (admin only) |

## Auth & Authorization
- **ASP.NET Core Identity** with custom `ApplicationUser` extending `IdentityUser`
- **Role-based** `[Authorize(Roles = "Admin")]` attributes on controllers
- **Session-based** cookie auth (default Identity behavior)
- **Seeded admin account** on first migration

## Implementation Order

1. **Project scaffolding** — ASP.NET Core MVC + Identity + Npgsql setup
2. **Database** — Entities, DbContext, migrations, seed data
3. **Authentication** — Login/Register with role selection
4. **Consultant features** — Time slot CRUD
5. **Customer features** — Browse consultants, book slots
6. **Admin features** — Dashboard, user management
7. **REST API** — JSON endpoints for each domain
8. **Polish** — UI styling, validation, error handling
