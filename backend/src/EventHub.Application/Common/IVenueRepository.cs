using EventHub.Domain.Entities;

namespace EventHub.Application.Common;

public interface IVenueRepository
{
    Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default);

    // Single WHERE Id IN (...) query, backing the GraphQL Event -> Venue DataLoader.
    Task<IReadOnlyList<Venue>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Venue venue, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
