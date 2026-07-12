using EventHub.Application.DTOs;
using EventHub.Domain.Common;

namespace EventHub.Application.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventDto>> GetAllAsync(CancellationToken cancellationToken = default);

    // Feeds HotChocolate's [UsePaging] middleware, which pushes Skip/Take to SQL.
    IQueryable<EventDto> GetEventsQueryable();

    Task<Result<EventDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<EventDto>> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default);
    Task<Result<EventDto>> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
