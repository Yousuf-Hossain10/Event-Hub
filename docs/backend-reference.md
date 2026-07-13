# Event Hub — Backend Reference (Steps 1-6)

A plain-language walkthrough of what's been built so far, why it works the way it does, and what to check if something behaves unexpectedly. Written to accompany `EventHub.postman_collection.json`.

---

## 1. The Shape of the System

```
Controllers/Resolvers  →  Application Services  →  Repositories  →  SQL Server
     (Api layer)              (business logic)      (Infrastructure)
```

Domain logic that must always hold true (an Event can't jump from Draft straight to Completed, a Booking can't exceed capacity) lives **on the entities themselves** (`Event.ChangeStatus()`, `Event.CanAcceptBooking()`) — not scattered across services or controllers. This means: if you ever wonder "where would I check whether this state transition is even allowed," the answer is always the same place — the entity.

---

## 2. Domain Model, As Actually Built

| Entity | What makes it interesting |
|---|---|
| **Venue** | Plain — Name, Address, MaxCapacity. No concurrency token (nothing about it is contentious enough to need one). |
| **Event** | Has a real state machine: `Draft → Published → Completed`, or `Draft/Published → Cancelled`. No other transition is legal. Carries a `RowVersion` (optimistic concurrency) — every update must include the RowVersion it was fetched with, or it's rejected. |
| **Attendee** | Minimal — Name, Email (unique). No REST endpoint exists to create these; they're seeded directly into the DB for testing. |
| **Booking** | Links an Attendee to an Event. Carries an `IdempotencyKey` (so retried requests are safe) and its own `RowVersion`. Has a uniqueness rule: one non-cancelled booking per (Event, Attendee) pair. |

All four support **soft delete** — deleting sets `IsDeleted = true` rather than removing the row, and a global query filter automatically hides soft-deleted rows from every query without you needing to remember to filter them out manually.

---

## 3. Why Two Different Concurrency Strategies Exist

This is the single most "ask me about this in an interview" detail in the backend.

- **Event updates use optimistic concurrency** (the `RowVersion` field). You fetch an Event, get back its current RowVersion, and must submit that same RowVersion when updating. If someone else updated it in between, your RowVersion is now stale and the update is rejected with a `409`. This fits because conflicting edits to an Event's title/description are rare — you don't want to pay a locking cost for something unlikely to collide.

- **Booking capacity uses pessimistic locking** (`UPDLOCK`/`ROWLOCK` at the database level, inside a transaction). When two people try to book the last seat at the exact same moment, the *second* request is made to physically wait until the first one finishes, then re-checks capacity with up-to-date numbers. This fits because contention here is exactly the scenario the whole feature exists to handle correctly — you'd rather briefly block a request than bounce a paying customer with a confusing conflict error.

Full reasoning: `docs/adr/ADR-003-concurrency-strategy-per-use-case.md`.

---

## 4. How Idempotency Actually Works (Booking Creation)

1. Client generates a random `IdempotencyKey` (a GUID) before submitting a new booking attempt.
2. Server first checks: "does a booking with this key already exist?" If yes, it returns *that* booking again (still `201`) — no new row is created, and the client can't tell the difference between "this just got created" and "this already existed."
3. As a safety net for the rare case where two requests with the *same* key arrive at the exact same instant (both passing the check above before either finishes), the database itself has a **unique index on IdempotencyKey** — so even if both requests try to insert, only one succeeds, and the code catches that specific failure and returns the winner's booking instead of erroring.

**Practical implication for testing:** reuse the same `idempotencyKey` value to test the "duplicate is prevented" behavior; use a fresh GUID for every *new* booking you actually want to succeed.

---

## 5. Result Pattern — What the Status Codes Mean

Every service method that can fail in an *expected* way (not a crash, a normal business outcome) returns a `Result<T>` rather than throwing. The API layer converts these into HTTP status codes consistently, and **every** `Result`-derived error response (404 / 409 / 422) has the same JSON body shape:

```json
{
  "code": "Booking.AlreadyBooked",
  "message": "Attendee '11111111-1111-1111-1111-111111111111' already has a confirmed booking for event '...'."
}
```

`code` is stable and is what client code should match against (e.g. to show a distinct message per failure reason, like the booking form in the Angular app does). `message` is human-readable and meant for logs/debugging — its wording isn't guaranteed to stay the same, so don't parse it.

| Situation | Status | Example code(s) |
|---|---|---|
| Referenced resource doesn't exist | `404` | `Event.NotFound`, `Venue.NotFound` |
| Request references something that doesn't exist, but the request itself is well-formed | `422` | `Event.VenueNotFound`, `Event.InvalidStatusTransition`, `Booking.EventNotFound`, `Booking.AttendeeNotFound` |
| A conflict with current state | `409` | `Event.ConcurrencyConflict` (stale RowVersion), `Booking.CannotAcceptBooking` (event full, or Draft/Cancelled), `Booking.AlreadyBooked` |
| Malformed input | `400` | *(not a Result error — see below)* |

**`400` is the one exception to the `{ code, message }` shape.** Malformed-input responses (e.g. a RowVersion string that isn't valid base64, or a missing required field) come from FluentValidation via ASP.NET Core's `ValidationProblem(ModelState)`, not from the Result pattern — they use the standard RFC 7807 `ProblemDetails` shape instead: `{ "title": "...", "status": 400, "errors": { "FieldName": ["message"] } }`. Don't expect a `code` field on a `400`.

Exceptions are reserved for things that are genuinely unexpected (a database connection dropping mid-request) — not for "the venue you referenced doesn't exist," which is a normal, anticipated outcome of user input.

---

## 6. REST vs. GraphQL — Why Both Exist

- **REST** handles all writes (Create/Update/Delete for Venue/Event, and the Booking mutation). Simpler to reason about for transactional writes, and it's what steps 3-4 already proved out before the trickiest logic (booking) was built.
- **GraphQL** (`/graphql`) is currently **read-only** — paginated Event listings with nested Venue data, without over- or under-fetching. It reuses the exact same `IEventService`/`IVenueService` as REST underneath, so there's no duplicated business logic — GraphQL is a different *shape* of the same operations, not a separate implementation.

**Why nested Venue data doesn't cause N+1 queries:** GraphQL's Venue lookups go through a **DataLoader**, which batches all the Venue ids needed for one page of Events into a single `WHERE Id IN (...)` query, instead of one query per Event. You can confirm this yourself by watching the SQL log while running a paginated query with nested venues — you should see exactly one Venues query per request, regardless of how many Events are on that page.

---

## 7. Testing Philosophy Applied Here

Two categories of tests exist, and they check different things:

- **Unit tests** (`EventHub.UnitTests`) — pure logic, no database. Things like: is `Draft → Completed` correctly rejected by `Event.ChangeStatus()`? Does `Result<T>.Value` correctly throw if you access it on a failed Result?
- **Integration tests** (`EventHub.IntegrationTests`) — spin up the real API against a real (separate) test database (`EventHubDb_IntegrationTests`, distinct from your dev database), and exercise actual HTTP calls. These catch things unit tests structurally can't — like whether a RowVersion conflict *actually* produces a 409 when two real HTTP requests race each other, not just whether the logic looks right on paper.

**A real lesson learned along the way:** a naive version of the concurrency race test passed even though it wasn't testing anything meaningful, because both simulated "concurrent" calls used a payload EF Core recognized as unchanged, so no actual UPDATE was ever issued. The fix was ensuring each competing request made a genuinely different change. This is a good example of why a passing test isn't proof of correctness by itself — you have to understand *why* it's passing.

---

## 8. Known, Deliberate Scope Boundaries (Not Bugs)

- No Attendee CRUD via REST — attendees are seeded directly in the database for this project's purposes.
- No `GET /api/bookings` or list endpoint — booking creation only, per the step's scope.
- REST list endpoints (`GET /api/events`, `GET /api/venues`) return a flat, unpaginated array — pagination is a GraphQL-only feature in this project, since that's where it was specified.

---

## 9. Quick Reference: Where Things Live

| Concept | File/Folder |
|---|---|
| Domain entities + state machine | `EventHub.Domain/Entities/` |
| Result/Error types | `EventHub.Domain/Common/` |
| DTOs | `EventHub.Application/DTOs/` |
| Services (business orchestration) | `EventHub.Application/Services/` |
| FluentValidation validators | `EventHub.Application/Validators/` |
| EF Core config (Fluent API) | `EventHub.Infrastructure/Persistence/Configurations/` |
| Repositories | `EventHub.Infrastructure/Repositories/` |
| REST controllers | `EventHub.Api/Controllers/` |
| GraphQL query type + DataLoader | `EventHub.Api/GraphQL/` |
| Architecture decisions | `docs/adr/` |
