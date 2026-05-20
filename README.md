# Consultify

A consultation booking platform built with ASP.NET Core 10 MVC. Consultants set available 30-minute time slots, customers browse and book consultations, and administrators oversee the entire system.

## Features

### Three Role-Based Dashboards

**Consultant**

- Create availability by picking a date + time range — slots are automatically split into 30-minute intervals
- View all time slots with booked/available/past status
- See upcoming appointments and mark them as completed
- Edit profile (bio, specialization, hourly rate, experience)

**Customer**

- Browse consultants with search, specialization filter, and rating display
- View consultant profiles with available slots for the next 14 days
- Book a 30-minute slot with concurrency-safe double-booking prevention
- Cancel appointments within a 24-hour window
- Leave reviews (1-5 rating + comment) on completed appointments

**Admin**

- Dashboard with system-wide statistics (users, consultants, appointments)
- Manage users — search, filter by role, activate/deactivate
- View and cancel any appointment
- Moderate reviews

### REST API

Full JSON API alongside MVC views. Endpoints include consultant listing, slot management, appointment booking/cancellation, and admin operations.

## Tech Stack

| Layer          | Technology                                      |
| -------------- | ----------------------------------------------- |
| Framework      | ASP.NET Core 10 MVC                             |
| Database       | PostgreSQL (via Entity Framework Core + Npgsql) |
| Authentication | ASP.NET Core Identity (cookie-based, roles)     |
| ORM            | Entity Framework Core (code-first)              |
| UI             | Razor Views + Bootstrap 5                       |
| Testing        | xUnit + Moq + EF Core InMemory                  |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) (local or remote)

### Setup

```bash
# Clone the repository
git clone <repo-url> Consultify
cd Consultify

# Configure database connection
# Edit src/Consultify.Web/appsettings.json with your PostgreSQL connection string

# Apply migrations
dotnet ef database update -p src/Consultify.Web

# Run the application
dotnet run --project src/Consultify.Web
```

Navigate to `https://localhost:5001` (or the URL shown in the terminal).

### Seed Accounts

| Role       | Email                           | Password       |
| ---------- | ------------------------------- | -------------- |
| Admin      | `admin@consultify.com`          | `Admin123!`    |
| Consultant | `sarah.chen@consultify.com`     | `Consult123!`  |
| Consultant | `marcus.johnson@consultify.com` | `Consult123!`  |
| Consultant | `priya.patel@consultify.com`    | `Consult123!`  |
| Customer   | `alice@example.com`             | `Customer123!` |
| Customer   | `bob@example.com`               | `Customer123!` |

Seed consultants come pre-loaded with 30-minute time slots for the next 7 business days (9:00 AM - 12:00 PM).

## Project Structure

```
Consultify/
├── src/Consultify.Web/
│   ├── Areas/
│   │   ├── Admin/              # /Admin/* routes
│   │   ├── Consultant/         # /Consultant/* routes
│   │   └── Customer/           # /Customer/* routes
│   ├── Controllers/            # Shared controllers (Home, Account)
│   ├── Api/Controllers/        # REST API endpoints
│   ├── Models/                 # Domain entities
│   ├── Services/               # Business logic layer
│   ├── Data/                   # DbContext, migrations, seed data
│   ├── Views/                  # Razor view templates
│   └── wwwroot/                # Static assets (CSS, JS, lib)
└── tests/Consultify.Web.Tests/ # Unit tests
```

## API Endpoints

| Method | Endpoint                              | Auth          | Description                                 |
| ------ | ------------------------------------- | ------------- | ------------------------------------------- |
| GET    | `/api/consultants`                    | Public        | List consultants (search, filter, paginate) |
| GET    | `/api/consultants/{id}`               | Public        | Consultant profile + average rating         |
| GET    | `/api/consultants/{id}/reviews`       | Public        | Reviews for a consultant                    |
| POST   | `/api/timeslots`                      | Consultant    | Create time slots (auto-split)              |
| DELETE | `/api/timeslots/{id}`                 | Consultant    | Delete own slot (unbooked only)             |
| GET    | `/api/appointments`                   | Authenticated | Get appointments (role-filtered)            |
| POST   | `/api/appointments`                   | Customer      | Book a slot                                 |
| PUT    | `/api/appointments/{id}/cancel`       | Authenticated | Cancel appointment                          |
| POST   | `/api/reviews`                        | Customer      | Submit review                               |
| GET    | `/api/admin/users`                    | Admin         | List all users                              |
| PUT    | `/api/admin/users/{id}/toggle-status` | Admin         | Activate/deactivate user                    |
| DELETE | `/api/admin/reviews/{id}`             | Admin         | Delete review                               |

All API responses use the `ApiResponse<T>` envelope:

```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed.",
  "errors": null
}
```

## Business Rules

1. **Double-booking prevention** — `RowVersion` concurrency token prevents two customers from booking the same slot simultaneously
2. **Slot deletion** — Consultants can only delete unbooked slots
3. **Cancellation window** — Customers can cancel up to 24 hours before start time
4. **Reviews** — Only on completed appointments, one review per appointment, 1-5 rating
5. **Profile deactivation** — Admin can disable any user; disabled users cannot log in
6. **Auto-split** — Time ranges are automatically divided into 30-minute slots; overlapping existing slots are skipped

## Running Tests

```bash
dotnet test tests/Consultify.Web.Tests
```

Tests use EF Core InMemory database (no PostgreSQL required) and cover:

- Slot availability filtering
- Double-booking prevention
- 24-hour cancellation enforcement
- 30-minute slot auto-split generation
- Slot deletion protection for booked slots

## Documentation

- `plan_consultify_4.md` — Full architecture plan
- `LOGBOOK.md` — Detailed build logbook
