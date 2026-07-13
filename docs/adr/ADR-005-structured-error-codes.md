# ADR-005: Structured `{ code, message }` Error Responses

**Status:** Accepted

**Date:** 2026-07-14

## Context

`ResultExtensions.ToErrorActionResult` (the single place every `Result`-derived 404/409/422
response is produced) originally passed `error.Message` — a free-text string — straight through
as the response body: `controller.Conflict(error.Message)`. That was sufficient while nothing
consumed the body programmatically; every check in this codebase up to that point only asserted
on the HTTP status code.

That stopped being true once the Angular booking form needed to show a *different* message for
each of three distinct `409`/`422` failures — event/attendee not found, event full, attendee
already booked (`BookingErrors.CannotAcceptBooking` vs. `BookingErrors.AlreadyBooked`, both
`409`s with different meanings). With a bare string body, the only way for the frontend to tell
these apart would have been inspecting the *wording* of `error.Message` — e.g. checking whether
it contains "already has a confirmed booking" vs. "cannot accept new bookings right now". `Error`
already carried a stable `Code` field (`Domain/Common/Error.cs`, used internally since the
Result pattern was introduced in ADR-002) that was simply never making it into the HTTP response.

## Decision

`ToErrorActionResult` now returns a small `ErrorResponse(string Code, string Message)` record as
the body instead of the bare `Message` string:

```json
{
  "code": "Booking.AlreadyBooked",
  "message": "Attendee '11111111-1111-1111-1111-111111111111' already has a confirmed booking for event '...'."
}
```

`Code` is the contract clients should depend on — it's the same string already used internally as
the `Error.Code` argument to every `Error.NotFound(...)`/`Error.Conflict(...)`/
`Error.Unprocessable(...)` factory call, so exposing it costs nothing new to maintain; it's just no
longer being thrown away at the API boundary. `Message` remains free text for logs/debugging and
is explicitly documented (in `docs/backend-reference.md` and the Postman collection) as not a
stable contract.

The Angular `EventDetail` booking form's `resolveBookingErrorMessage()` is the concrete consumer:
it switches on `err.status` plus `(err.error as ApiErrorResponse)?.code` to pick one of three
hardcoded, translatable UI messages — never on `err.error.message`.

This is a global change to every Result-derived error response (Venues, Events, Bookings), not a
Booking-only fix, since introducing two different body shapes for the "same kind" of error
depending on which controller produced it would be a worse inconsistency than the one being fixed.
`400` responses are the deliberate exception — those come from FluentValidation via
`ValidationProblem(ModelState)`, a separate mechanism producing the standard RFC 7807
`ProblemDetails` shape, and were left alone; unifying that too would be a larger, unrelated change
to how model validation errors are reported.

## Alternatives Considered

- **Leave the bare string, have the frontend match on message substrings.** Rejected — this is
  the exact fragility being avoided. `message` text is meant to read naturally as a sentence
  ("Attendee 'x' already has a confirmed booking for event 'y'"), which means minor rewording
  (fixing a typo, improving clarity, localizing it) silently breaks any client-side string match
  with no compile-time or test-time signal that it happened. A `code` field is designed to be
  matched on and won't casually change.
- **HTTP status code alone, no body-level discrimination.** Rejected — this is what the codebase
  had before Booking's `AlreadyBooked` case existed, and it's exactly what broke: `409` alone
  can't distinguish "event full" from "already booked," since both are legitimate 409s for the
  same endpoint. Status code says *category* of failure (client error vs. conflict vs.
  unprocessable); it was never meant to carry *which specific* failure within that category.
- **Full RFC 7807 `ProblemDetails` for every error, including Result-derived ones**, using its
  `type`/`title`/`detail`/`extensions` fields to carry the code (e.g. in `extensions.code`).
  Considered since `400`s already use this shape via `ValidationProblem`. Rejected for now as
  more ceremony than the current need justifies — `ProblemDetails` is designed for cases wanting
  a machine-readable `type` URI plus rich `extensions`, and adopting it project-wide would be a
  reasonable direction (worth revisiting once there are more consumers than one Angular form), but
  a two-field record was the minimum change that solved the actual, current problem.

## Consequences

- Every `Result`-derived 404/409/422 response body changed shape at once (bare string →
  `{ code, message }`). No consumers outside this codebase existed yet, so this wasn't a breaking
  change to any real client — but it's worth remembering that this endpoint contract has now
  shipped and any *future* format change here would need the same care a real breaking API change
  requires.
- Frontend error handling reads as a lookup table (`status` + `code` → UI message) instead of
  regex/substring matching against prose — verified live for all three Booking failure cases plus
  the plain-404 case, each producing the correct distinct message.
- New failure reasons only need a new `Error.Xxx(...)` factory method with a distinct `Code` — no
  API-layer changes are needed to expose it, since `ToErrorActionResult` already forwards whatever
  code the `Error` carries.
- `message` is explicitly *not* a contract — this needs to stay true going forward. If a future
  change ever needs client code to depend on message wording, that's a sign a new `code` should be
  introduced instead, not that `message` should be treated as newly stable.

---
