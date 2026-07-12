using EventHub.Application.Common;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Repositories;

public class AttendeeRepository(EventHubDbContext context) : IAttendeeRepository
{
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Attendees.AnyAsync(a => a.Id == id, cancellationToken);
}
