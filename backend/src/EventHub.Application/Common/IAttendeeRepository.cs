namespace EventHub.Application.Common;

public interface IAttendeeRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
