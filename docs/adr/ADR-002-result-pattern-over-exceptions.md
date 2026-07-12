# ADR-002: Result Pattern Over Exceptions for Expected Failures

**Status:** Accepted

**Date:** 2026-07-12

## Context

CLAUDE.md §5.3 sets a non-negotiable practice: a `Result<T>` type with typed failure reasons for
*expected* failures (a lookup that doesn't exist, a state transition that isn't allowed, a
concurrent edit that lost a race), reserving exceptions for truly exceptional cases. Build Order
step 3 (REST CRUD) predates `Result<T>` existing in the codebase, so at that point Event's
"referenced Venue doesn't exist" case was signaled with a purpose-built `VenueNotFoundException`,
caught in the controller and mapped to a 400. That was flagged at the time as an interim stand-in,
not the final shape.

Step 4 introduced domain logic on entities (`Event.ChangeStatus`, `Event.CanAcceptBooking`) and
needed a way for entity methods to report a failed state transition without throwing — throwing
from `ChangeStatus` would make every caller wrap every status change in a try/catch for what is a
routine, expected outcome (an organizer's "Publish" click landing on an already-cancelled event).
The concurrency token work in the same step had the identical shape: a stale `RowVersion` on
`Update` is not a bug, it's an expected outcome of concurrent editing that the API needs to report
distinctly from "not found" or "malformed request."

## Decision

Added `Result`, `Result<TValue>`, `Error`, and `ErrorType` to `EventHub.Domain/Common`. `Error`
carries a `Code`, a `Message`, and an `ErrorType` (`NotFound`, `Conflict`, `Unprocessable`,
`Failure`). Application services (`EventService`, `VenueService`) return `Result<T>`/`Result` from
every method with a real failure mode (`GetById`, `Update`, `Delete`) and reuse the same type from
domain-level checks (`Event.ChangeStatus` returns `Result`, using an `Event.Errors` static class for
the transition-invariant error). A single `ResultExtensions.ToErrorActionResult` in the Api layer
maps `ErrorType` to an HTTP status uniformly across both controllers, so no controller action
hand-picks a status code for a given failure. `VenueNotFoundException` was deleted and replaced with
`EventErrors.VenueNotFound`, an `ErrorType.Unprocessable` error that flows through the same
mechanism as every other expected failure, landing on 422 instead of the exception path's 400.

`Result`/`Error` live in **Domain**, not Application, specifically so `Event`'s own methods can
return them without Domain depending on Application (Application already depends on Domain, so
placing the primitive at the bottom of the dependency graph lets every layer above reuse the same
type without inversion).

## Alternatives Considered

- **Keep `VenueNotFoundException` alongside `Result<T>`.** Rejected. Having two idioms for the same
  category of problem (expected, client-triggerable failure) means every new failure case requires
  a fresh decision — "does this get a Result or an exception?" — with no principled answer, and every
  controller action needs to know which mechanism a given service method uses before it can handle
  errors correctly. The two mechanisms also don't compose: a method returning `Result<EventDto>`
  that can *also* throw `VenueNotFoundException` has two failure channels a caller must check.
  Retiring the exception the moment `Result<T>` existed to replace it removed that inconsistency
  entirely rather than let it linger as legacy alongside the new pattern.
- **A third-party Result library (e.g. FluentResults, OneOf, ErrorOr).** Rejected for scope: the
  actual requirement is narrow (a handful of failure types, one HTTP-mapping point), and CLAUDE.md's
  own example (`Result<T>` with typed reasons like `EventFull`, `AlreadyBooked`) describes exactly
  the hand-rolled shape implemented here. Pulling in a dependency for ~60 lines of code would be the
  kind of unnecessary abstraction CLAUDE.md's engineering practices explicitly discourage.
- **ProblemDetails-only, no `Result<T>`.** i.e., let services throw domain-specific exceptions and
  centralize translation to `ProblemDetails` in a global exception-handling middleware. Rejected
  because it still uses exceptions as control flow for routine, expected outcomes, which is the
  exact anti-pattern CLAUDE.md's non-negotiable practice calls out — exceptions carry stack-capture
  cost and make the failure paths invisible in a method's signature, unlike `Result<T>` which forces
  every caller to see and handle failure at the call site.

## Consequences

- Every Application service method with a real failure mode has a `Result`-shaped signature, so a
  caller cannot forget to handle a failure the way a caller can ignore an undocumented exception.
- Adding a new HTTP status mapping (e.g. a future `ErrorType.Forbidden` for JWT auth in step 8) is a
  one-line addition to `ResultExtensions.ToErrorActionResult`, not a new catch block per controller.
- `Result<T>.Value` throws `InvalidOperationException` if accessed on a failed result — this is a
  genuine "should never happen if the caller checked `IsSuccess` first" case, which is exactly the
  class of problem exceptions are still appropriate for; `Result<T>` doesn't eliminate exceptions,
  it confines them to programmer-error cases instead of expected business outcomes.
- GraphQL resolvers (step 5) reuse the exact same `Result<T>`-returning service methods as REST,
  translating a failure to `GraphQLException(result.Error.Message)` — one Application-layer failure
  model serves both API surfaces instead of a parallel GraphQL-specific error type.
- The trade-off: `Result<T>` adds a small amount of ceremony (`Result.Success(x)` /
  `Result.Failure<T>(error)` wrapping) to every service method versus a plain return value or a
  thrown exception. That ceremony is the explicit cost of making failure paths visible in the type
  system, which is the entire point of the pattern.

---
