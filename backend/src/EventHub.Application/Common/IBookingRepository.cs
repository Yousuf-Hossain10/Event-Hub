using EventHub.Domain.Entities;

namespace EventHub.Application.Common;

public interface IBookingRepository
{
    Task<Booking?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default);

    // Must be called while holding the caller's lock on the Event row (see IEventRepository.GetForBookingAsync)
    // for the count to be race-safe.
    Task<int> CountConfirmedForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    // Single grouped query for displaying counts across a list of events (e.g. EventService.GetAllAsync);
    // not lock-protected, since it's a read-only display value, not a capacity-gating decision.
    Task<IReadOnlyDictionary<Guid, int>> CountConfirmedByEventIdsAsync(
        IReadOnlyList<Guid> eventIds,
        CancellationToken cancellationToken = default);

    // Same locking requirement as CountConfirmedForEventAsync.
    Task<bool> HasConfirmedBookingAsync(Guid eventId, Guid attendeeId, CancellationToken cancellationToken = default);

    Task<BookingCreationOutcome> TryCreateAsync(Booking booking, CancellationToken cancellationToken = default);
}
