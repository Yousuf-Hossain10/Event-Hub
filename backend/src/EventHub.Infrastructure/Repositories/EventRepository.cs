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

    public IQueryable<Event> Query() => context.Events.AsNoTracking();

    public Task<Event?> GetForBookingAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        context.Events
            .FromSqlInterpolated($"SELECT * FROM Events WITH (UPDLOCK, ROWLOCK) WHERE Id = {eventId}")
            .SingleOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Event @event, CancellationToken cancellationToken = default) =>
        await context.Events.AddAsync(@event, cancellationToken);

    public void SetOriginalRowVersion(Event @event, byte[] rowVersion) =>
        context.Entry(@event).Property(e => e.RowVersion).OriginalValue = rowVersion;

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
