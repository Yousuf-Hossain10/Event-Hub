using EventHub.Application.Common;
using EventHub.Application.DTOs;
using EventHub.Domain.Common;
using EventHub.Domain.Entities;

namespace EventHub.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository,
    IAttendeeRepository attendeeRepository,
    IUnitOfWork unitOfWork) : IBookingService
{
    public async Task<Result<BookingDto>> CreateAsync(CreateBookingDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await bookingRepository.GetByIdempotencyKeyAsync(dto.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(ToDto(existing));
        }

        if (!await attendeeRepository.ExistsAsync(dto.AttendeeId, cancellationToken))
        {
            return Result.Failure<BookingDto>(BookingErrors.AttendeeNotFound(dto.AttendeeId));
        }

        // Holds an UPDLOCK on the Event row for the lifetime of this transaction, so a concurrent
        // request for the same event blocks here instead of both reading the same confirmed count.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var @event = await eventRepository.GetForBookingAsync(dto.EventId, cancellationToken);
        if (@event is null)
        {
            return Result.Failure<BookingDto>(BookingErrors.EventNotFound(dto.EventId));
        }

        if (await bookingRepository.HasConfirmedBookingAsync(dto.EventId, dto.AttendeeId, cancellationToken))
        {
            return Result.Failure<BookingDto>(BookingErrors.AlreadyBooked(dto.EventId, dto.AttendeeId));
        }

        var confirmedCount = await bookingRepository.CountConfirmedForEventAsync(dto.EventId, cancellationToken);
        if (!@event.CanAcceptBooking(confirmedCount))
        {
            return Result.Failure<BookingDto>(BookingErrors.CannotAcceptBooking(dto.EventId));
        }

        var booking = Booking.Create(Guid.NewGuid(), dto.EventId, dto.AttendeeId, dto.IdempotencyKey);
        var outcome = await bookingRepository.TryCreateAsync(booking, cancellationToken);

        if (outcome == BookingCreationOutcome.DuplicateIdempotencyKey)
        {
            // Lost a race against a concurrent request with the same IdempotencyKey; return its booking.
            var winner = await bookingRepository.GetByIdempotencyKeyAsync(dto.IdempotencyKey, cancellationToken);
            return Result.Success(ToDto(winner!));
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToDto(booking));
    }

    private static BookingDto ToDto(Booking booking) => new(
        booking.Id,
        booking.EventId,
        booking.AttendeeId,
        booking.Status,
        booking.CreatedAt,
        booking.IdempotencyKey);
}
