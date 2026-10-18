using Sprints.Users.Application.Abstractions;


namespace Sprints.Users.Infrastructure.DataAccess;


internal sealed class NullTransaction : ITransaction
{
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
