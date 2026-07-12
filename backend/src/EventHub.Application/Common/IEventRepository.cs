using EventHub.Domain.Entities;

namespace EventHub.Application.Common;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken = default);

    // Untracked, composable source for GraphQL cursor pagination; callers must apply their own ordering.
    IQueryable<Event> Query();

    // Pessimistic row lock (SQL Server UPDLOCK/ROWLOCK); must be called inside an active transaction
    // so the lock is held until commit/rollback, serializing concurrent capacity checks for this event.
    Task<Event?> GetForBookingAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task AddAsync(Event @event, CancellationToken cancellationToken = default);
    void SetOriginalRowVersion(Event @event, byte[] rowVersion);

    // Returns false on an optimistic concurrency conflict instead of throwing.
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
