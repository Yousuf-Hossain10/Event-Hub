using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs;

public record BookingDto(
    Guid Id,
    Guid EventId,
    Guid AttendeeId,
    BookingStatus Status,
    DateTime CreatedAt,
    Guid IdempotencyKey);
