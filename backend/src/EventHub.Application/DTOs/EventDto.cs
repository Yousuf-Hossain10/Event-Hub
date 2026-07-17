using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs;

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDate,
    int Capacity,
    EventStatus Status,
    Guid VenueId,
    string RowVersion,
    int ConfirmedBookingCount);
