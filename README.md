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
- [Caching](#caching)
- [Rate Limiting](#rate-limiting)
- [Messaging (RabbitMQ)](#messaging-rabbitmq)
- [Audit Logging](#audit-logging)
- [Idempotency](#idempotency)
- [Structured Logging](#structured-logging)
- [Correlation IDs](#correlation-ids)
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
| Cache | Redis (StackExchange.Redis) |
| Message broker | RabbitMQ 4 |
| Auth | JWT Bearer, HttpOnly cookies |
| Real-time | SignalR |
| Logging | Serilog (compact JSON) |
| API Docs | Swashbuckle / OpenAPI 3 |
| Frontend | React 19, TypeScript, Vite |
| Styling | Tailwind CSS 4 |
| Calendar UI | FullCalendar 6 |
| Unit tests | xUnit, NSubstitute |
| E2E tests | Playwright (NUnit) |

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
│  Filters                            │
│  └── [Idempotent] action filter     │
│                                     │
│  Services                           │
│  ├── TokenService (JWT)             │
│  ├── CardEncryptionService (AES)    │
│  ├── AuditLogger                    │
│  └── IdempotencyService             │
│                                     │
│  Messaging                          │
│  ├── RabbitMqPublisher              │
│  └── BookingConfirmationConsumer    │
│                                     │
│  Hubs                               │
│  └── TicketingHub (SignalR)         │
└──────────┬───────────────┬──────────┘
           │ EF Core       │ StackExchange.Redis
           ▼               ▼
┌──────────────────┐  ┌──────────────────┐
│ SQLite Database  │  │  Redis Cache     │
│ Events, Orders,  │  │  events:all      │
│ Users, Cards,    │  │  (2 min TTL)     │
│ AuditLogs,       │  └──────────────────┘
│ IdempotencyKeys  │
└──────────────────┘
           │ AMQP
           ▼
┌──────────────────────────────────────┐
│  RabbitMQ                            │
│  booking-confirmed queue             │
│  (durable, at-least-once delivery)   │
└──────────────────────────────────────┘
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
│   ├── Filters/                  # [Idempotent] action filter
│   ├── Hubs/                     # SignalR hub
│   ├── Messaging/                # RabbitMQ publisher + consumer
│   ├── Middleware/               # Correlation ID middleware
│   ├── Models/                   # EF Core entity classes
│   ├── Services/                 # TokenService, AuditLogger, IdempotencyService, ...
│   └── Program.cs                # App bootstrap + DI registration
│
├── EventTicketing.Tests/         # xUnit unit + integration tests
│   ├── Controllers/              # Controller unit tests (in-memory SQLite)
│   ├── Fakes/                    # In-memory test doubles (publisher, audit logger, ...)
│   ├── Filters/                  # Action filter unit tests
│   ├── Integration/              # WebApplicationFactory integration tests
│   ├── Messaging/                # RabbitMQ publisher + consumer unit tests
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

AuditLogs                     IdempotencyKeys
  id                            id
  action                        key
  entityType                    requestPath
  entityId (nullable)           statusCode
  userId (nullable)             responseBody
  userEmail (nullable)          createdAt
  details (nullable JSON)       expiresAt
  timestamp

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
| POST | `/api/events/{id}/book` | Optional | Book a ticket — supports `Idempotency-Key` header |
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

## Caching

The event listing (`GET /api/events`) is cached in **Redis** with a 2-minute absolute TTL. The cache key is `EventTicketing:events:all`.

The cache is explicitly invalidated whenever an event is created, updated, or deleted, so changes made by admins are visible immediately. The 2-minute TTL is a safety net: if invalidation somehow fails (e.g. a crash mid-request), stale data disappears on its own.

Reads: check Redis first → on miss, query SQLite and populate cache.

In integration tests, Redis is replaced with `IDistributedMemoryCache` so tests run without a Redis instance.

---

## Rate Limiting

Two layers of rate limiting protect the API:

| Limiter | Scope | Limit | Algorithm |
|---|---|---|---|
| Global | All endpoints, per IP | 100 requests / minute | Fixed window |
| Booking | `POST /api/events/{id}/book`, per IP | 5 requests / minute | Sliding window |

The booking-specific limiter uses a sliding window to prevent burst buying at the start of each window. Requests that exceed the limit receive `429 Too Many Requests`.

---

## Messaging (RabbitMQ)

After a ticket is successfully booked, a `BookingConfirmedMessage` is published to the `booking-confirmed` RabbitMQ queue. A `BookingConfirmationConsumer` BackgroundService consumes messages from this queue.

**Design decisions:**

- **Best-effort publishing** — if RabbitMQ is unavailable when a booking is made, the error is logged and the booking still succeeds. The client always receives a `201 Created`.
- **Durable queue** — the queue survives broker restarts; messages are not lost if the consumer is temporarily offline.
- **At-least-once delivery** — the consumer uses `autoAck: false` and only acknowledges a message after processing it. If the API crashes mid-processing, the message is redelivered.

The `booking-confirmed` queue is the foundation for future downstream processing (confirmation emails, analytics, fraud detection) — each new consumer subscribes independently without touching the booking code.

In integration tests, `IMessagePublisher` is replaced with an in-memory `FakeMessagePublisher` and the `BookingConfirmationConsumer` hosted service is removed, so tests run without a broker.

---

## Audit Logging

Every significant action is recorded in the `AuditLogs` table via `IAuditLogger`:

| Action | Trigger |
|---|---|
| `EventCreated` | Admin creates an event |
| `EventUpdated` | Admin updates an event |
| `EventDeleted` | Admin deletes an event |
| `TicketBooked` | A ticket is booked successfully |
| `UserLoggedIn` | Successful login |
| `UserLoginFailed` | Failed login attempt (wrong password or unknown email) |

Each log entry captures: action, entity type, entity ID, user ID (nullable), user email, optional JSON details, and a UTC timestamp. Failed login attempts with an unknown email record the attempted email without a user ID.

---

## Idempotency

The `POST /api/events/{id}/book` endpoint supports an optional `Idempotency-Key` header. When provided:

- The first request is processed normally and the response (status code + body) is stored in the `IdempotencyKeys` table with a **24-hour TTL**.
- Any subsequent request with the same key and path returns the cached response immediately — no second booking is created, no seats are decremented again.
- Keys are scoped to `(key, requestPath)` — the same key cannot be reused across different endpoints.

This protects against double bookings caused by network timeouts and client retries. If the header is absent, the request is processed normally with no idempotency guarantees.

### Known limitation — concurrent in-flight requests

The idempotency check is a read-then-write operation: the filter calls `GetAsync`, finds no cached entry, lets the request proceed, then calls `StoreAsync`. If two requests carrying the **same key** arrive simultaneously and both pass the `GetAsync` check before either has written its result, both will create a booking. The `DbUpdateException` thrown by the second `StoreAsync` (the unique index on `(Key, RequestPath)` rejects the duplicate) is swallowed, so the caller gets a correct-looking response — but two orders have been created.

This race window is narrow in practice, but it is a real gap. It will be closed as part of the **Outbox pattern** step, which will add a **database-level unique constraint on `(UserId, EventId)`** (or a pessimistic lock) so the booking itself is atomic regardless of how many in-flight requests slip through the idempotency check.

---

## Structured Logging

All logs are emitted as **compact JSON** via [Serilog](https://serilog.net/), making them easy to ingest into log aggregation systems (Datadog, Elastic, etc.).

Every HTTP request is logged automatically by `UseSerilogRequestLogging()`, including method, path, status code, and elapsed time. Application-level log calls (`LogInformation`, `LogError`, etc.) appear alongside request logs in the same structured format.

---

## Correlation IDs

Every request is assigned a correlation ID, propagated via the `X-Correlation-ID` header. If the client sends the header, the value is reused; otherwise a new UUID is generated.

The correlation ID is:
- Added to the **response** headers so clients can reference it when reporting issues
- Added to the **Serilog log context** so every log line emitted during that request includes the ID

This makes it straightforward to trace all log lines belonging to a single request across the application.

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
- Redis (`brew install redis && brew services start redis`)
- RabbitMQ (`brew install rabbitmq && brew services start rabbitmq`)

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

### RabbitMQ Management UI

To monitor queues and messages locally, enable the management plugin once:

```bash
rabbitmq-plugins enable rabbitmq_management
brew services restart rabbitmq
# Management UI: http://localhost:15672 (guest / guest)
```

---

## Testing

### Unit + Integration tests (xUnit)

```bash
dotnet test EventTicketing.Tests
```

Covers controllers, services, action filters, and integration tests that boot the full app via `WebApplicationFactory` against a temporary SQLite database. Redis and RabbitMQ are replaced with in-memory fakes so no external services are required.

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
