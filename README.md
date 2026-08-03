# Event Ticketing

A full-stack event ticketing platform where users browse and book tickets for events, and admins manage the event catalogue. Built as a portfolio project demonstrating production-ready backend patterns on top of a real-time capable API.

---

## Table of Contents

- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Data Model](#data-model)
- [API Endpoints](#api-endpoints)
- [Authentication & Authorization](#authentication--authorization)
- [Real-Time Updates (SignalR)](#real-time-updates-signalr)
- [OpenAPI / Swagger](#openapi--swagger)
- [Health Checks](#health-checks)
- [Running Locally](#running-locally)
- [Testing](#testing)
- [Seeded Demo Accounts](#seeded-demo-accounts)

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 9, C# |
| ORM | Entity Framework Core 9 |
| Database | SQLite (local) |
| Auth | JWT Bearer, HttpOnly cookies |
| Real-time | SignalR |
| API Docs | Swashbuckle / OpenAPI 3 |
| Frontend | React 19, TypeScript, Vite |
| Styling | Tailwind CSS 4 |
| Calendar UI | FullCalendar 6 |
| Unit tests | xUnit |
| E2E tests | Playwright (NUnit, Java) |

---

## Architecture

```
┌─────────────────────────────────────┐
│         React Frontend              │
│  (Vite dev server: localhost:5173)  │
│                                     │
│  - Event list, detail, booking      │
│  - Admin CRUD, calendar, orders     │
│  - SignalR client (real-time seats) │
└──────────┬──────────────────────────┘
           │ HTTP + WebSocket
           ▼
┌─────────────────────────────────────┐
│       ASP.NET Core 9 API            │
│       (localhost:5017)              │
│                                     │
│  Controllers                        │
│  ├── EventsController               │
│  ├── AuthController                 │
│  └── AdminController                │
│                                     │
│  Services                           │
│  ├── TokenService (JWT)             │
│  └── CardEncryptionService (AES)    │
│                                     │
│  Hubs                               │
│  └── TicketingHub (SignalR)         │
└──────────┬──────────────────────────┘
           │ EF Core
           ▼
┌─────────────────────────────────────┐
│       SQLite Database               │
│  Events, Orders, Users, Cards       │
└─────────────────────────────────────┘
```

The frontend and API are separate processes during development. In production they can be served from the same host — the API serves static files built from the React app.

The API is stateless: all session state lives in a short-lived JWT stored in an HttpOnly cookie. No server-side sessions.

---

## Project Structure

```
EventTicketing.sln
├── EventTicketing.Api/           # ASP.NET Core Web API
│   ├── Controllers/              # HTTP endpoints
│   ├── Data/                     # EF Core DbContext + migrations + seeders
│   ├── Hubs/                     # SignalR hub
│   ├── Middleware/               # Correlation ID middleware
│   ├── Models/                   # EF Core entity classes
│   ├── Services/                 # TokenService, CardEncryptionService
│   └── Program.cs                # App bootstrap + DI registration
│
├── EventTicketing.Tests/         # xUnit unit + integration tests
│   ├── Controllers/              # Controller unit tests (in-memory SQLite)
│   ├── Integration/              # WebApplicationFactory integration tests
│   └── Services/                 # Service unit tests
│
├── EventTicketing.E2ETests/      # Playwright E2E tests (NUnit)
│   ├── HomePageTests.cs
│   ├── EventDetailTests.cs
│   └── AdminPageTests.cs
│
├── EventTicketing.Scheduler/     # Utility: refreshes event dates for the demo
│
└── event-ticketing-ui/           # React + TypeScript frontend
    └── src/
        ├── components/           # Page and UI components
        ├── context/              # AuthContext (React context)
        └── hooks/                # useHubEvents (SignalR), useAuth
```

---

## Data Model

```
Users ──────────┐
  id            │ (nullable)
  name          │
  email         ├──── Orders ──────────── Events
  passwordHash  │       id                  id
  role          │       eventId             title
                │       userId ◄────────    description
Cards           │       email              startTime
  id            │       price              endTime
  userId ◄──────┘       bookedAt           venue
  encryptedNumber                          totalSeats
  last4                                    availableSeats *
  expiryDate                               price
  cardType                                 imageUrl
  isDefault                                eventType

* AvailableSeats has a [ConcurrencyCheck] attribute — EF Core uses
  optimistic locking to prevent two simultaneous bookings from
  overselling the last seat.
```

---

## API Endpoints

### Auth — `/api/auth`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | None | Login, sets HttpOnly JWT cookie |
| POST | `/api/auth/logout` | None | Clears the auth cookie |
| GET | `/api/auth/me` | Required | Current user profile |
| GET | `/api/auth/me/orders` | Required | Current user's booking history |
| GET | `/api/auth/me/cards/default` | Required | Saved default payment card |
| PUT | `/api/auth/me/cards/default` | Required | Save or update default card |

### Events — `/api/events`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/events` | None | List all events |
| GET | `/api/events/{id}` | None | Single event detail |
| POST | `/api/events` | Admin | Create event |
| PUT | `/api/events/{id}` | Admin | Update event |
| DELETE | `/api/events/{id}` | Admin | Delete event |
| POST | `/api/events/{id}/book` | Optional | Book a ticket (email required) |
| GET | `/api/events/orders` | Required | Orders by email |
| GET | `/api/events/orders/{orderId}` | None | Single order detail |

### Admin — `/api/admin`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/admin/orders` | Admin | All orders across all events |
| GET | `/api/admin/summary` | Admin | Revenue and seat summary per event |

---

## Authentication & Authorization

Authentication uses **JWT Bearer tokens** stored in **HttpOnly cookies**.

**Login flow:**
1. Client posts email and password to `POST /api/auth/login`
2. API verifies password hash using ASP.NET Core's `PasswordHasher`
3. `TokenService` generates a signed JWT (8-hour expiry) with claims: user ID, email, name, role
4. Token is written to an `auth_token` HttpOnly cookie (`Secure` + `SameSite=Strict` in production)
5. Client receives user info in the response body; the cookie is sent automatically on subsequent requests

**Why HttpOnly cookies instead of localStorage:**
The token is never accessible to JavaScript, which prevents XSS attacks from stealing it.

**Swagger / API client access:**
The JWT middleware also accepts a standard `Authorization: Bearer <token>` header, so Swagger UI and non-browser clients can authenticate by pasting the token directly.

**Authorization levels:**
- Public — no auth required (event listing, event detail, booking)
- Authenticated — valid JWT required (`[Authorize]`)
- Admin — valid JWT with `role = "admin"` claim required (`[Authorize(Roles = "admin")]`)

**Payment card encryption:**
Card numbers are encrypted with AES-256 before being stored. Only the last 4 digits are stored in plain text for display. The encryption key is loaded from configuration and must never be committed to source control.

---

## Real-Time Updates (SignalR)

The API hosts a SignalR hub at `/hubs/ticketing`. The frontend connects on page load and listens for the following server-pushed events:

| Event | Payload | Trigger |
|---|---|---|
| `BookingMade` | `eventId` | A ticket is booked |
| `EventCreated` | `eventId` | Admin creates an event |
| `EventUpdated` | `eventId` | Admin updates an event |
| `EventDeleted` | `eventId` | Admin deletes an event |

When the frontend receives any of these, it re-fetches the affected event so available seat counts and event lists stay current without polling.

---

## OpenAPI / Swagger

The API ships with a full **OpenAPI 3.0 specification** generated automatically from controller annotations using [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore).

**How it works:**

1. `AddSwaggerGen()` in `Program.cs` registers Swashbuckle and configures the spec metadata (title, version, description)
2. `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in the `.csproj` tells the compiler to emit an XML file containing all `/// <summary>` comments from controllers
3. `options.IncludeXmlComments(xmlPath)` feeds that XML file into Swashbuckle so endpoint descriptions appear in the UI
4. `[ProducesResponseType]` attributes on each action tell Swashbuckle what HTTP status codes each endpoint can return, which populates the "Responses" section of the spec
5. A JWT Bearer security scheme is registered so protected endpoints show a padlock icon and you can authenticate directly in the UI

**Accessing the UI:**

| Environment | URL |
|---|---|
| Local development | `http://localhost:5017/swagger` |

**Authenticating in Swagger UI:**
1. Call `POST /api/auth/login` with valid credentials
2. Copy the JWT from the `auth_token` cookie (browser DevTools → Application → Cookies)
3. Click the **Authorize** button (top right), paste the token, click **Authorize**
4. All subsequent requests in the UI will include the token in the `Authorization` header

The spec is generated from code on every build — it cannot drift out of sync with the actual API.

---

## Health Checks

The API exposes two health check endpoints that can be used by load balancers, container orchestrators (Kubernetes), or uptime monitors to verify the application is running correctly.

### Endpoints

| Endpoint | Purpose | What it checks |
|---|---|---|
| `GET /health` | **Liveness** | Is the process alive and able to respond? |
| `GET /health/ready` | **Readiness** | Is the app ready to serve traffic? (includes DB) |

### Responses

| Status | HTTP Code | Body |
|---|---|---|
| All checks pass | `200 OK` | `Healthy` |
| Any check fails | `503 Service Unavailable` | `Unhealthy` |

### How it works

`AddHealthChecks().AddDbContextCheck<AppDbContext>()` registers a database health check tagged with `"ready"`. On each request to `/health/ready`, EF Core runs a lightweight test query against SQLite to verify the database is reachable and responsive.

The liveness endpoint (`/health`) runs all registered checks. The readiness endpoint (`/health/ready`) is filtered to only run checks tagged `"ready"`, which in this case is the database check. This distinction matters in container environments:

- **Liveness failure** → container is restarted
- **Readiness failure** → container is temporarily removed from the load balancer rotation without being restarted (e.g. during a slow DB startup)

### Example usage

```bash
# Check if the app is alive
curl http://localhost:5017/health

# Check if the app and database are ready to serve traffic
curl http://localhost:5017/health/ready
```

---

## Running Locally

### Prerequisites
- .NET 9 SDK
- Node.js 20+

### Backend

```bash
cd EventTicketing.Api
dotnet run
# API: http://localhost:5017
# Swagger: http://localhost:5017/swagger
```

The database is created and seeded automatically on first run.

### Frontend

```bash
cd event-ticketing-ui
npm install
npm run dev
# UI: http://localhost:5173
```

The Vite dev server proxies `/api` and `/hubs` requests to `localhost:5017`.

---

## Testing

### Unit + Integration tests (xUnit)

```bash
dotnet test EventTicketing.Tests
```

Covers controllers, services, and integration tests that boot the full app via `WebApplicationFactory` against a temporary SQLite database.

### E2E tests (Playwright)

Requires both the API and frontend dev server to be running.

```bash
dotnet test EventTicketing.E2ETests
```

Covers the homepage event listing, event detail and booking flow, and the full admin CRUD workflow.

---

## Seeded Demo Accounts

The database is seeded with the following accounts on first run. All use the password `Password`.

| Email | Role |
|---|---|
| `john@example.com` | User |
| `jane@example.com` | User |
| `alex@example.com` | User |
| `admin@example.com` | Admin |

Each account is pre-seeded with a demo Visa card ending in `4242`.
