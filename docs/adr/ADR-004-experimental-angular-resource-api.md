# ADR-004: Using the Experimental Angular `resource()`/`rxResource()` API

**Status:** Accepted

**Date:** 2026-07-14

## Context

`EventDetail` needs to reactively re-fetch an `Event` and its `Venue` whenever the `:id` route
param changes — including navigating from one event detail page straight to another, without an
intermediate list visit, where Angular's default `RouteReuseStrategy` reuses the same component
instance rather than destroying and recreating it. An earlier version of this component read
`ActivatedRoute.snapshot.paramMap` once in `ngOnInit`, which broke exactly this case: the snapshot
is captured once per component instance, so a second same-route navigation left the page showing
the first event's data under the second event's URL. That bug and its fix are documented in the
conversation that produced this component; this ADR covers the specific mechanism chosen to fix
it, not the bug itself.

The fix requires two things: (1) a reactive way to observe the route param — Angular's
`input()` bound via `withComponentInputBinding()` — and (2) a reactive way to re-run an HTTP
fetch (twice, chained: `Event` first, then `Venue` keyed off the resolved `Event.venueId`)
whenever that input changes, including correctly cancelling an in-flight request if the input
changes again before it resolves.

`rxResource()` (and its Promise-based sibling `resource()`) is Angular's built-in answer to
exactly this second requirement. Checking the installed Angular 21 type definitions directly
(`node_modules/@angular/core/types/_api-chunk.d.ts` and `rxjs-interop.d.ts`) rather than
relying on possibly-stale training knowledge confirmed two things: the API shape has changed
since its original 19.0 preview (current signature uses `params`/`stream`, not the earlier
`request`/`loader` naming some documentation and tutorials still show), and every exported
symbol in this area — `resource`, `rxResource`, `Resource`, `ResourceRef`, `ResourceOptions`,
etc. — is still annotated `@experimental`, not `@stable`.

## Decision

Use `rxResource()` for both the `Event` fetch and the chained `Venue` fetch in `EventDetail`,
despite the `@experimental` marking:

```typescript
protected readonly eventResource = rxResource({
  params: () => this.id(),
  stream: ({ params: id }) => this.eventService.getEventById(id),
});

protected readonly venueResource = rxResource({
  params: () => this.eventResource.value()?.venueId,
  stream: ({ params: venueId }) => this.venueService.getVenueById(venueId),
});
```

`venueResource` stays in its `idle` state (no HTTP request fires) until `eventResource.value()`
resolves and `.venueId` becomes defined — the chain is expressed declaratively through the
`params` function rather than an imperative `switchMap` pipeline, and dependent-resource chaining
this way is a supported, documented use of the API, not an incidental trick.

## Alternatives Considered

- **`toObservable(id).pipe(switchMap(...))` writing to local signals manually** — the stable
  fallback. `toObservable` (from `@angular/core/rxjs-interop`) is not marked experimental, and
  `switchMap` gives the same cancel-on-new-value semantics `rxResource` provides internally. This
  was the leading alternative and remains the fallback if `rxResource` changes incompatibly before
  it stabilizes. Not chosen for this slice because it requires manually declaring and updating
  `loading`/`error`/`value` signals in the `subscribe` handlers (see the previous iteration of this
  component before `rxResource` was introduced) — `rxResource` gives `.value()`, `.isLoading()`,
  `.error()`, and `.status()` for free, and chaining a second dependent fetch off the first's
  resolved value needs an extra nested `switchMap` plus a null/undefined guard, versus one more
  `rxResource` block here.
- **Keeping `ActivatedRoute.snapshot`** — rejected outright; this is the bug being fixed, not an
  alternative to weigh against `rxResource`.
- **A third-party async-state library** (e.g. TanStack Query's Angular adapter) — rejected for
  scope. Angular ships a first-party answer to this exact problem; reaching for an external
  dependency to avoid one experimental core API would be a worse trade than the risk being
  hedged against.

## Consequences

- `@experimental` in Angular's own convention means the API can change shape or be removed in a
  future minor version without the usual deprecation window `@stable`/`@deprecated` APIs get. If
  that happens, every `rxResource(...)` call site (currently just the two in `EventDetail`) needs
  to migrate to the `toObservable`/`switchMap` fallback above — the blast radius is intentionally
  contained to this one component for now, since it's the only place this pattern has been used.
- A real testing gotcha, already hit and fixed once: `rxResource` integrates with Angular's
  pending-tasks tracking, so `fixture.whenStable()` in a test with `provideHttpClientTesting()`
  hangs forever waiting for a mocked HTTP request that's never flushed. `event-detail.spec.ts`
  uses `fixture.detectChanges()` instead — this is a permanent constraint on how any future test
  touching a `rxResource`-backed component must be written, not a one-off workaround.
- In exchange for the experimental-API risk: less code per fetch (no manual signal wiring),
  automatic request cancellation on rapid param changes, and declarative dependent-resource
  chaining that reads as data-flow rather than imperative subscription management — verified live
  against the actual bug scenario (client-side navigation between two event detail pages) rather
  than assumed to work from the API shape alone.
- If Angular stabilizes `resource()`/`rxResource()` in a later version (tracked informally by
  watching the `@experimental` tag on future Angular upgrades), no action is needed here beyond
  removing the "still experimental" caveat from this ADR's context — the call sites don't change.

---
