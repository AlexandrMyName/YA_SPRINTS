using Microsoft.EntityFrameworkCore.Storage;
using SprintsASP_NetCore_API.Application.Abstractions; 


namespace SprintsASP_NetCore_API.Infrastructure.DataAccess;


internal sealed class EfTransaction : ITransaction
{

    private readonly IDbContextTransaction _tx;

    public EfTransaction(IDbContextTransaction tx) => _tx = tx;

    public Task CommitAsync(CancellationToken ct = default) => _tx.CommitAsync(ct);
    public Task RollbackAsync(CancellationToken ct = default) => _tx.RollbackAsync(ct);
    public ValueTask DisposeAsync() => _tx.DisposeAsync();
}
