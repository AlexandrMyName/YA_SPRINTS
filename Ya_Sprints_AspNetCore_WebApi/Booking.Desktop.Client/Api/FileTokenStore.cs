// FileTokenStore.cs
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sprints.Desktop.Api;

/// <summary>
/// Простое хранилище токенов в %AppData%/Sprints.Desktop/auth.dat.
/// 
///  !!!  Production: используй OS keychain —
///   Windows:  DPAPI (ProtectedData.Protect)
///   macOS:    Keychain
///   Linux:    libsecret / GNOME Keyring
/// </summary>
public sealed class FileTokenStore : ITokenStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileTokenStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "Sprints.Desktop");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "auth.dat");
    }

    public async Task<TokenData?> LoadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath)) return null;
            var bytes = await File.ReadAllBytesAsync(_filePath, ct);
            if (bytes.Length == 0) return null;
            return JsonSerializer.Deserialize<TokenData>(bytes);
        }
        catch
        {
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(TokenData data, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(data);
            await File.WriteAllBytesAsync(_filePath, bytes, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
        }
        finally
        {
            _lock.Release();
        }
    }
}