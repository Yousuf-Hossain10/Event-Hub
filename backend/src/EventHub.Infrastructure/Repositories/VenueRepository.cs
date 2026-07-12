using EventHub.Application.Common;
using EventHub.Domain.Entities;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Repositories;

public class VenueRepository(EventHubDbContext context) : IVenueRepository
{
    public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Venues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Venues.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Venue>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) =>
        await context.Venues.Where(v => ids.Contains(v.Id)).AsNoTracking().ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Venues.AnyAsync(v => v.Id == id, cancellationToken);

    public async Task AddAsync(Venue venue, CancellationToken cancellationToken = default) =>
        await context.Venues.AddAsync(venue, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
