using EventHub.Application.Common;
using EventHub.Application.DTOs;
using EventHub.Domain.Common;
using EventHub.Domain.Entities;

namespace EventHub.Application.Services;

public class EventService(IEventRepository eventRepository, IVenueRepository venueRepository) : IEventService
{
    public async Task<IReadOnlyList<EventDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.GetAllAsync(cancellationToken);
        return events.Select(ToDto).ToList();
    }

    public IQueryable<EventDto> GetEventsQueryable() =>
        eventRepository.Query()
            .OrderBy(e => e.StartDate)
            .ThenBy(e => e.Id)
            .Select(e => new EventDto(
                e.Id,
                e.Title,
                e.Description,
                e.StartDate,
                e.Capacity,
                e.Status,
                e.VenueId,
                Convert.ToBase64String(e.RowVersion)));

    public async Task<Result<EventDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken);
        return @event is null
            ? Result.Failure<EventDto>(EventErrors.NotFound(id))
            : Result.Success(ToDto(@event));
    }

    public async Task<Result<EventDto>> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        if (!await venueRepository.ExistsAsync(dto.VenueId, cancellationToken))
        {
            return Result.Failure<EventDto>(EventErrors.VenueNotFound(dto.VenueId));
        }

        var @event = Event.Create(
            Guid.NewGuid(),
            dto.Title,
            dto.Description,
            dto.StartDate,
            dto.Capacity,
            dto.Status,
            dto.VenueId);

        await eventRepository.AddAsync(@event, cancellationToken);
        await eventRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(@event));
    }

    public async Task<Result<EventDto>> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (@event is null)
        {
            return Result.Failure<EventDto>(EventErrors.NotFound(id));
        }

        if (!await venueRepository.ExistsAsync(dto.VenueId, cancellationToken))
        {
            return Result.Failure<EventDto>(EventErrors.VenueNotFound(dto.VenueId));
        }

        var statusChange = @event.ChangeStatus(dto.Status);
        if (statusChange.IsFailure)
        {
            return Result.Failure<EventDto>(statusChange.Error);
        }

        @event.Title = dto.Title;
        @event.Description = dto.Description;
        @event.StartDate = dto.StartDate;
        @event.Capacity = dto.Capacity;
        @event.VenueId = dto.VenueId;

        eventRepository.SetOriginalRowVersion(@event, Convert.FromBase64String(dto.RowVersion));

        var saved = await eventRepository.SaveChangesAsync(cancellationToken);
        if (!saved)
        {
            return Result.Failure<EventDto>(EventErrors.ConcurrencyConflict);
        }

        return Result.Success(ToDto(@event));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (@event is null)
        {
            return Result.Failure(EventErrors.NotFound(id));
        }

        @event.IsDeleted = true;

        var saved = await eventRepository.SaveChangesAsync(cancellationToken);
        return saved ? Result.Success() : Result.Failure(EventErrors.ConcurrencyConflict);
    }

    private static EventDto ToDto(Event @event) => new(
        @event.Id,
        @event.Title,
        @event.Description,
        @event.StartDate,
        @event.Capacity,
        @event.Status,
        @event.VenueId,
        Convert.ToBase64String(@event.RowVersion));
}
