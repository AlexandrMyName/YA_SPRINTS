

namespace Sprints.Bookings.Application.Abstractions;


/// <summary>
/// Нейтральная транзакция. Реализация — Infrastructure.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
