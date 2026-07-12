using EventHub.Domain.Entities;

namespace EventHub.Application.Common;

public interface IBookingRepository
{
    Task<Booking?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default);

    // Must be called while holding the caller's lock on the Event row (see IEventRepository.GetForBookingAsync)
    // for the count to be race-safe.
    Task<int> CountConfirmedForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<BookingCreationOutcome> TryCreateAsync(Booking booking, CancellationToken cancellationToken = default);
}
