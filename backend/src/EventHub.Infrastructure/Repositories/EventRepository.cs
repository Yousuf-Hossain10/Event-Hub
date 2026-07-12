using EventHub.Application.Common;
using EventHub.Domain.Entities;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Repositories;

public class EventRepository(EventHubDbContext context) : IEventRepository
{
    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Events.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Event @event, CancellationToken cancellationToken = default) =>
        await context.Events.AddAsync(@event, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
