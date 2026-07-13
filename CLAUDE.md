# Event Hub — Project Context for Claude Code

This file is auto-loaded as persistent context. Read it fully before making changes. Follow it strictly — do not substitute alternate frameworks, patterns, or libraries without asking first.

---

## 1. What This Project Is

A content-driven event management platform: organizers create Events/Venues, attendees browse, filter, and book. Built as a portfolio project to demonstrate enterprise-grade .NET + Angular full-stack skills for a Developer role at an enterprise CMS company (Optimizely-style stack: ASP.NET MVC, headless content, REST/GraphQL APIs).

**Scope discipline matters more than feature count.** Do not add features beyond what's specified below (no payments, no email sending, no multi-tenancy, no admin dashboards) unless explicitly asked.

---

## 2. Tech Stack (fixed — do not substitute)

| Layer | Choice |
|---|---|
| Backend | ASP.NET Core 8 Web API |
| API styles | REST (CRUD) + GraphQL via HotChocolate |
| ORM | EF Core (Fluent API configuration, not data annotations) |
| Database | SQL Server (LocalDB/Express for local dev) |
| Frontend | Angular 21 — standalone components, signals for state, Reactive Forms |
| Auth | JWT with role separation (Organizer vs Attendee) |
| Validation | FluentValidation |
| Resilience | Polly (retry policy on external calls) |
| Logging | Serilog, structured, with correlation IDs |
| Testing | xUnit (backend), Jasmine/Karma or Jest (frontend) |
| CI/CD | GitHub Actions |
| Hosting (target) | Azure App Service + Azure SQL Database, Angular via Azure Static Web Apps |

---

## 3. Architecture

```
Client Layer
 ├─ Razor/MVC Pages (server-rendered event listing, static content)
 └─ Angular SPA module (booking flow, live filters, animations)
        │ REST                    │ GraphQL
        ▼                         ▼
ASP.NET Core 8 Web API
 ├─ Controllers (REST)   ├─ HotChocolate Resolvers (GraphQL, with DataLoader)
 └─ Application Services (business logic, Result<T> pattern)
        │
    EF Core Repositories
        │
    SQL Server
```

**Layering rule:** Controllers/Resolvers → Application Services → Repositories. Business/domain logic lives on domain entities themselves (e.g. `Event.CanAcceptBooking()`), never in controllers.

**Why this shape:** MVC/Razor demonstrates the ASP.NET MVC requirement; Angular demonstrates modern JS framework requirement; GraphQL + REST both demonstrate the JD's explicit "loading content using REST APIs or GraphQL" line.

---

## 3.5 Angular Project Notes (as actually scaffolded)

- Generated via `ng new` with Angular CLI 21.2.19, standalone components, SCSS, routing enabled, no SSR/SSG.
- Component file naming in this Angular version: `app.ts` / `app.html` / `app.scss` (not the older `app.component.ts` convention). When generating new components, follow whatever naming the Angular CLI produces by default — do not manually rename to the older `.component.ts` style.
- Folder structure under `src/app/`:
  - `core/services/` — singleton services that call the backend API (e.g. `event.service.ts`, `booking.service.ts`)
  - `core/models/` — TypeScript interfaces mirroring backend DTOs (e.g. `event.model.ts` matching `EventDto`)
  - `core/guards/` — route guards (used from step 8 onward for JWT-protected routes)
  - `core/interceptors/` — HTTP interceptors (used from step 8 onward to attach the JWT token to outgoing requests)
  - `features/events/` — event listing/detail components and their local logic
  - `features/bookings/` — booking flow components
  - `shared/components/` — small reusable presentational components (e.g. loading spinner, buttons)
- Use signals for local component state where reasonable. Use Reactive Forms (not template-driven forms) for the booking form.
- Before assuming zoneless change detection is or isn't active, check `app.config.ts` / `main.ts` for how change detection was configured during scaffolding rather than guessing.
- REST calls use Angular's `HttpClient`. GraphQL calls (paginated event listing with nested venue data) should use a lightweight approach (plain `HttpClient` POST to `/graphql` with a query string is sufficient — do not introduce Apollo Client or a heavy GraphQL client library unless explicitly asked, to keep the frontend dependency footprint small).

---

## 4. Domain Model

| Entity | Key Fields | Relationships |
|---|---|---|
| Event | Id, Title, Description, StartDate, Capacity, Status, RowVersion (concurrency token) | belongs to Venue, has many Bookings |
| Venue | Id, Name, Address, MaxCapacity | has many Events |
| Attendee | Id, Name, Email | has many Bookings |
| Booking | Id, EventId, AttendeeId, Status, CreatedAt, IdempotencyKey | links Event ↔ Attendee |

Keep the schema exactly this size. Depth over breadth.

---

## 5. Non-Negotiable Engineering Practices

These are what separate this project from a bootcamp-tutorial clone. Implement fully rather than skip:

1. **Optimistic concurrency** — `RowVersion` on Event/Booking; two simultaneous booking attempts on the last seat must not both succeed.
2. **Idempotency key** on the booking mutation — retried requests must not create duplicate bookings.
3. **Result pattern** (`Result<T>` with typed failure reasons like `EventFull`, `AlreadyBooked`) for expected failures — exceptions reserved for truly exceptional cases only.
4. **DataLoader** in the GraphQL layer for nested field resolution (Event → Venue) to avoid N+1 queries.
5. **Cursor-based pagination** on the event listing query, not offset/skip-take.
6. **Domain logic on entities**, not in controllers or services (e.g. `CanAcceptBooking()`).
7. **Soft deletes** via EF Core global query filters, not hard deletes.
8. **DTOs only** across the wire — never serialize EF entities directly.
9. **Nullable reference types enabled** and respected project-wide.
10. **Structured logging** with correlation IDs and meaningful context (EventId, BookingId, UserId in log scope), not generic string messages.

If time runs short, it's acceptable to fully implement a subset of these (concurrency + idempotency + DataLoader + Result pattern + strong edge-case tests is the minimum bar) and document the rest as "next steps" in the README — but do not silently skip them without noting it.

---

## 6. Working Agreement (how to collaborate with me on this)

- **Work in vertical slices.** One feature/layer at a time. Do not implement multiple unrelated features in a single pass.
- **Stop and summarize** what you changed and why after each slice, before continuing to the next.
- **Ask before deviating** from this file — different library, different pattern, different folder structure, etc.
- **Never silently swallow errors.** Every catch block should either rethrow, return a Result failure, or log with context — and briefly note which choice was made and why.
- **Write tests alongside features**, not as an afterthought pass at the end — especially edge cases (full event, double-booking, past-event booking, concurrent booking race).
- **Use meaningful commit messages** describing the slice, not generic "update files."

---

## 7. Build Order (reference — see full plan doc for detail)

1. Backend scaffold (layered folder structure, no logic yet)
2. Domain entities + EF Core + Fluent API config + SQL Server connection + initial migration
3. REST CRUD for Events/Venues (DTOs, no raw entities)
4. Domain logic on entities + Result pattern + concurrency token
5. GraphQL layer + DataLoader + cursor pagination
6. Booking mutation with idempotency + concurrency handling + edge-case tests
7. Angular frontend (standalone components, signals, Reactive Forms) wired to REST + GraphQL
8. JWT auth with role separation
9. Logging, health checks, CI pipeline, deployment

---

## 8. Repo Structure

See `/docs` for architecture decision records (ADRs) — log significant decisions there as you make them (e.g. why Result pattern over exceptions, why EF Core over Dapper) so they can be referenced later.
