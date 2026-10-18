// ITokenStore.cs
using System.Threading;
using System.Threading.Tasks;


namespace Sprints.Desktop.Api;


public interface ITokenStore
{

    Task<TokenData?> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(TokenData data, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}