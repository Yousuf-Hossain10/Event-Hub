namespace EventHub.Application.Common;

public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    // DisposeAsync without a prior CommitAsync rolls back, mirroring EF Core's IDbContextTransaction.
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
