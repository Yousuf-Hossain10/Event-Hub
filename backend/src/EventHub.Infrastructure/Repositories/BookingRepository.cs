using EventHub.Application.Common;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Repositories;

public class BookingRepository(EventHubDbContext context) : IBookingRepository
{
    public Task<Booking?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default) =>
        context.Bookings.FirstOrDefaultAsync(b => b.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<int> CountConfirmedForEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        context.Bookings.CountAsync(b => b.EventId == eventId && b.Status == BookingStatus.Confirmed, cancellationToken);

    public Task<bool> HasConfirmedBookingAsync(Guid eventId, Guid attendeeId, CancellationToken cancellationToken = default) =>
        context.Bookings.AnyAsync(
            b => b.EventId == eventId && b.AttendeeId == attendeeId && b.Status == BookingStatus.Confirmed,
            cancellationToken);

    public async Task<BookingCreationOutcome> TryCreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        try
        {
            await context.Bookings.AddAsync(booking, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return BookingCreationOutcome.Created;
        }
        catch (DbUpdateException ex) when (IsDuplicateIdempotencyKey(ex))
        {
            return BookingCreationOutcome.DuplicateIdempotencyKey;
        }
    }

    private static bool IsDuplicateIdempotencyKey(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
