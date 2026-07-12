# ADR-003: Concurrency Strategy Per Use Case

**Status:** Accepted

**Date:** 2026-07-13

## Context

The codebase now has two genuinely different concurrency problems, introduced in different Build
Order steps, and it would be easy for a future contributor to assume "we already solved
concurrency" and reach for the wrong tool:

- **Step 4 — editing a single `Event`.** Two organizers (or one organizer with two open tabs)
  might `PUT /api/events/{id}` against the same row. The invariant is "the row I'm about to
  overwrite hasn't changed since I read it." This is a classic single-row edit-conflict problem:
  contention is rare (organizers rarely edit the same event at the same instant), and when it does
  happen, the correct behavior is to reject the *second* writer and let the client re-fetch and
  retry with current data.
- **Step 6 — booking against `Event.Capacity`.** Two attendees racing for the last seat on a
  popular event is not a single-row edit conflict — the `Event` row's own columns (`Title`,
  `Capacity`, `Status`, ...) aren't being changed by a booking at all. The invariant being
  protected is `COUNT(*) FROM Bookings WHERE EventId = @id AND Status = Confirmed < Event.Capacity`,
  a value computed across a *different, unbounded set of rows* at the moment of insert. This is a
  high-contention scenario by construction — the whole point of "the last seat" is that multiple
  requests are competing for it at once.

These are different shapes of problem, and CLAUDE.md's own instruction for step 6 named a
transaction explicitly ("Use `Event.CanAcceptBooking()` as part of this, inside a transaction"),
which is a pessimistic-locking signal, not an optimistic one.

## Decision

- **Event updates: optimistic concurrency**, via the existing `RowVersion` (SQL Server
  `rowversion`) concurrency token. The client echoes back the `RowVersion` it read;
  `EventRepository.SetOriginalRowVersion` asserts it as the tracked entity's original value before
  `SaveChangesAsync`, so EF Core's generated `UPDATE ... WHERE Id = @id AND RowVersion = @original`
  fails (0 rows affected → `DbUpdateConcurrencyException` → caught and translated to `409`) if
  someone else wrote to the row first. No locks are held; readers and writers never block each
  other.
- **Booking capacity: pessimistic locking**, via `SELECT * FROM Events WITH (UPDLOCK, ROWLOCK)
  WHERE Id = @id`, executed inside an explicit transaction (`IUnitOfWork.BeginTransactionAsync`).
  A concurrent booking request for the *same* event blocks at that `SELECT` until the first
  transaction commits or rolls back, so the "count confirmed bookings → `CanAcceptBooking()` →
  insert" sequence is atomic per event. This directly produces "two simultaneous requests for the
  last seat must not both succeed," which optimistic concurrency cannot guarantee for a
  count-based invariant without an artificial version column standing in for the count (see
  Alternatives).

`Booking` also carries a `RowVersion` column (added in the step 2 follow-up) even though no
`Booking` update/cancel endpoint exists yet. It's dormant today. When a future cancel/update flow
is built, it should use the *same* optimistic strategy as `Event` — a booking-status edit is a
single-row conflict, not a capacity-counting one, so the reasoning in this ADR points at optimistic
concurrency for that future work, not pessimistic locking.

## Alternatives Considered

- **Optimistic concurrency for booking too** — e.g. add a denormalized `SeatsRemaining` (or
  `BookingCount`) column on `Event`, bump it as part of the booking write guarded by `RowVersion`,
  and retry on conflict. Rejected: it requires a new column that duplicates information already
  derivable from `COUNT(*) FROM Bookings` (drift risk — the two could disagree if anything ever
  writes to `Bookings` without going through this exact code path), and it pushes the retry loop
  into application code. Optimistic concurrency degrades badly under high contention — the retry
  count under contention scales with the number of simultaneous writers, and "the last seat on a
  popular event" is *precisely* the high-contention case, i.e. optimistic retry is worst exactly
  where this feature needs to be correct.
- **Pessimistic locking for Event updates too** — take an `UPDLOCK` before every `Event` edit.
  Rejected: organizer edits are low-contention by nature (two people editing the identical field of
  the identical event at the identical instant is rare), so paying a locking cost on every write to
  guard against an almost-never-occurring conflict is the wrong trade — optimistic concurrency lets
  concurrent reads and unrelated writes proceed without blocking and only pays a cost on the rare
  actual conflict.
- **`SERIALIZABLE` isolation globally** (or per-request) instead of a targeted row lock. Rejected as
  a blunt instrument — it would serialize unrelated transactions across the whole database (booking
  Event A would block editing Venue B), not just the specific critical section that needs
  protecting. The `UPDLOCK, ROWLOCK` hint scopes the lock to exactly one `Event` row, so booking
  requests for *different* events never contend with each other.

## Consequences

- Two different concurrency idioms now coexist in the codebase, and picking the right one requires
  recognizing which shape of invariant is being protected: a single row's version (optimistic) vs.
  an aggregate computed across a related table (pessimistic, scoped to the owning row). This ADR is
  the reference for that decision the next time a similar question comes up (e.g. a future
  `Venue.MaxCapacity` vs. aggregate `Event` count check would face the same fork).
  See also ADR-002 (`ADR-002-result-pattern-over-exceptions.md`) for how both paths' failures
  surface as `Result<T>` rather than uncaught exceptions.
- Optimistic concurrency on `Event` is non-blocking and near-zero-cost in the common case, at the
  cost of surfacing a `409` to the losing client, who must re-fetch and retry — acceptable because
  that's a rare, user-facing, recoverable event (an organizer editing a field).
- Pessimistic locking on booking means concurrent booking requests for the *same* event serialize
  through the critical section — this is the entire point, not a side effect, but it means the
  transaction body must stay minimal (fetch, count, insert only; no external calls or slow work)
  or it becomes a throughput bottleneck for that event's booking rate. Requests against *different*
  events are unaffected — the lock is row-scoped.
- If a single event's booking volume ever needs to scale past what row-level locking can serialize
  (e.g. a mega-event with thousands of concurrent attempts at doors-open), the mitigation is a
  queue-based reservation system in front of the booking write, not deeper/broader locking — flagged
  here as a known future concern, explicitly out of scope for the current step.

---
