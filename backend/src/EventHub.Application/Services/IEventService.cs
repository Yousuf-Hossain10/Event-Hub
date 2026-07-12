using EventHub.Application.DTOs;

namespace EventHub.Application.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <exception cref="Common.VenueNotFoundException">The referenced VenueId does not exist.</exception>
    Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default);

    /// <exception cref="Common.VenueNotFoundException">The referenced VenueId does not exist.</exception>
    Task<EventDto?> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
