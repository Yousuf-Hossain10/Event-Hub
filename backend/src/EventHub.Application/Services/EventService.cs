using EventHub.Application.Common;
using EventHub.Application.DTOs;
using EventHub.Domain.Entities;

namespace EventHub.Application.Services;

public class EventService(IEventRepository eventRepository, IVenueRepository venueRepository) : IEventService
{
    public async Task<IReadOnlyList<EventDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.GetAllAsync(cancellationToken);
        return events.Select(ToDto).ToList();
    }

    public async Task<EventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken);
        return @event is null ? null : ToDto(@event);
    }

    public async Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        if (!await venueRepository.ExistsAsync(dto.VenueId, cancellationToken))
        {
            throw new VenueNotFoundException(dto.VenueId);
        }

        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            StartDate = dto.StartDate,
            Capacity = dto.Capacity,
            Status = dto.Status,
            VenueId = dto.VenueId
        };

        await eventRepository.AddAsync(@event, cancellationToken);
        await eventRepository.SaveChangesAsync(cancellationToken);

        return ToDto(@event);
    }

    public async Task<EventDto?> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (@event is null)
        {
            return null;
        }

        if (!await venueRepository.ExistsAsync(dto.VenueId, cancellationToken))
        {
            throw new VenueNotFoundException(dto.VenueId);
        }

        @event.Title = dto.Title;
        @event.Description = dto.Description;
        @event.StartDate = dto.StartDate;
        @event.Capacity = dto.Capacity;
        @event.Status = dto.Status;
        @event.VenueId = dto.VenueId;

        await eventRepository.SaveChangesAsync(cancellationToken);

        return ToDto(@event);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (@event is null)
        {
            return false;
        }

        @event.IsDeleted = true;
        await eventRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static EventDto ToDto(Event @event) => new(
        @event.Id,
        @event.Title,
        @event.Description,
        @event.StartDate,
        @event.Capacity,
        @event.Status,
        @event.VenueId);
}
