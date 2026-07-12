using EventHub.Domain.Entities;
using EventHub.Domain.Enums;

namespace EventHub.UnitTests.Domain;

public class EventTests
{
    private static Event CreateDraftEvent(DateTime? startDate = null, int capacity = 10) =>
        Event.Create(
            Guid.NewGuid(),
            "Title",
            "Description",
            startDate ?? DateTime.UtcNow.AddDays(1),
            capacity,
            EventStatus.Draft,
            Guid.NewGuid());

    [Theory]
    [InlineData(EventStatus.Draft, EventStatus.Published)]
    [InlineData(EventStatus.Draft, EventStatus.Cancelled)]
    [InlineData(EventStatus.Published, EventStatus.Cancelled)]
    [InlineData(EventStatus.Published, EventStatus.Completed)]
    public void ChangeStatus_AllowsValidTransitions(EventStatus from, EventStatus to)
    {
        var @event = CreateDraftEvent();
        if (from != EventStatus.Draft)
        {
            @event.ChangeStatus(from);
        }

        var result = @event.ChangeStatus(to);

        Assert.True(result.IsSuccess);
        Assert.Equal(to, @event.Status);
    }

    [Theory]
    [InlineData(EventStatus.Draft, EventStatus.Completed)]
    [InlineData(EventStatus.Cancelled, EventStatus.Published)]
    [InlineData(EventStatus.Completed, EventStatus.Published)]
    [InlineData(EventStatus.Completed, EventStatus.Cancelled)]
    public void ChangeStatus_RejectsInvalidTransitions(EventStatus from, EventStatus to)
    {
        var @event = CreateDraftEvent();
        if (from != EventStatus.Draft)
        {
            @event.ChangeStatus(EventStatus.Published);
            if (from != EventStatus.Published)
            {
                @event.ChangeStatus(from);
            }
        }

        var result = @event.ChangeStatus(to);

        Assert.True(result.IsFailure);
        Assert.Equal(from, @event.Status);
    }

    [Fact]
    public void ChangeStatus_SameStatus_IsANoOpSuccess()
    {
        var @event = CreateDraftEvent();

        var result = @event.ChangeStatus(EventStatus.Draft);

        Assert.True(result.IsSuccess);
        Assert.Equal(EventStatus.Draft, @event.Status);
    }

    [Fact]
    public void CanAcceptBooking_ReturnsTrue_WhenPublishedFutureAndUnderCapacity()
    {
        var @event = CreateDraftEvent(startDate: DateTime.UtcNow.AddDays(1), capacity: 10);
        @event.ChangeStatus(EventStatus.Published);

        Assert.True(@event.CanAcceptBooking(currentBookingCount: 9));
    }

    [Fact]
    public void CanAcceptBooking_ReturnsFalse_WhenAtCapacity()
    {
        var @event = CreateDraftEvent(startDate: DateTime.UtcNow.AddDays(1), capacity: 10);
        @event.ChangeStatus(EventStatus.Published);

        Assert.False(@event.CanAcceptBooking(currentBookingCount: 10));
    }

    [Fact]
    public void CanAcceptBooking_ReturnsFalse_WhenNotPublished()
    {
        var @event = CreateDraftEvent(startDate: DateTime.UtcNow.AddDays(1), capacity: 10);

        Assert.False(@event.CanAcceptBooking(currentBookingCount: 0));
    }

    [Fact]
    public void CanAcceptBooking_ReturnsFalse_WhenStartDateHasPassed()
    {
        var @event = CreateDraftEvent(startDate: DateTime.UtcNow.AddDays(-1), capacity: 10);
        @event.ChangeStatus(EventStatus.Published);

        Assert.False(@event.CanAcceptBooking(currentBookingCount: 0));
    }
}
