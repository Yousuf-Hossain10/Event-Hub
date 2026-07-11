# ADR-001: Guid Primary Keys

**Status:** Accepted

**Date:** 2026-07-12

## Context

Every entity (`Event`, `Venue`, `Attendee`, `Booking`) needs a primary key type. The two realistic
options for a SQL Server + EF Core stack are:

- `int`/`long` with database identity (auto-increment)
- `Guid`, generated client-side or server-side

`Booking.IdempotencyKey` is already a client-supplied `Guid` (a retried request must be able to
present the same key before the server has assigned any identity), so the key type decision also
needs to sit comfortably next to that field.

## Decision

Use `Guid` for every entity's primary key (`Event.Id`, `Venue.Id`, `Attendee.Id`, `Booking.Id`),
mapped to SQL Server's `uniqueidentifier` type via EF Core's default conventions (no custom value
generator configured yet).

## Alternatives Considered

- **`int` identity** — smaller (4 bytes vs 16), monotonically increasing so it clusters well and
  keeps the primary key's B-tree append-only with no page splits. Rejected because: IDs would be
  guessable/enumerable (an attendee could increment a booking ID and probe other attendees'
  bookings before authorization is even checked), and it doesn't compose well with a
  disconnected/offline client generating an ID before the row exists (relevant for the booking
  flow's idempotency key, and for the Angular SPA optimistically rendering a created entity before
  the server round-trip completes).
- **`long` identity** — same trade-offs as `int`, just with more headroom; doesn't solve the
  guessability or client-generation problems.

## Consequences

- **Index size / clustering performance, which a senior reviewer should assume I'm aware of:**
  `Guid.NewGuid()` produces version-4 (random) GUIDs, not sequential ones. Used as a clustered
  primary key, random GUIDs cause the clustered index to receive inserts at random points in the
  B-tree rather than at the end — this drives page splits, index fragmentation, and worse buffer
  cache locality than an identity column, and the 16-byte key is also 4x the width of an `int`,
  which grows every non-clustered index that carries the clustering key as its row locator. At
  this project's scale (a portfolio app, not a high-write production system) this is not a
  practical problem, but it's a known, deliberate trade-off, not an oversight.
- **Mitigation available if this ever needs to scale:** switch to sequential GUID generation
  (`NEWSEQUENTIALID()` as a SQL Server column default, or a client-side sequential/COMB GUID
  generator) to get roughly-ordered inserts while keeping the non-enumerable, client-generatable
  properties that motivated this decision. Not adopted now — no `HasDefaultValueSql` /
  `ValueGeneratedOnAdd` override is configured, so `Guid.NewGuid()` values are expected to be
  assigned before an entity is added to the `DbContext` (this will matter once step 4 introduces
  entity construction/factory logic).
- IDs are safe to expose in URLs/DTOs without leaking row counts or allowing sequential enumeration
  across tenants or entity types.
- Every entity's ID type is consistent with `Booking.IdempotencyKey`, so idempotency-key comparison
  and primary-key comparison use the same CLR type end-to-end.

---
