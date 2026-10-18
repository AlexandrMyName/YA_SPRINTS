 
using System;


namespace Sprints.Desktop.Api;


public sealed record TokenData
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public DateTime ExpiresAtUtc { get; init; }
    public Guid UserId { get; init; }
    public string Login { get; init; } = "";
    public bool IsAdmin { get; init; }
}