using EventHub.Domain.Entities;

namespace EventHub.Application.Common;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Event @event, CancellationToken cancellationToken = default);
    void SetOriginalRowVersion(Event @event, byte[] rowVersion);

    // Returns false on an optimistic concurrency conflict instead of throwing.
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
