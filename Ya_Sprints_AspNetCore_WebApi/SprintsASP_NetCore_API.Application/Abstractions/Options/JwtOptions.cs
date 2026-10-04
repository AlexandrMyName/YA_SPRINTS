namespace SprintsASP_NetCore_API.Application.Abstractions.Options;


public class JwtOptions
{

    public const string SectionName = "Jwt";

    /// <summary>
    /// "RS256" или "HS256" — какой алгоритм использовать для подписи.
    /// </summary>
    public string Mode { get; set; } = "RS256";

    public string PrivateKeyPath { get; set; } = "keys/private.pem";
    public string PublicKeyPath { get; set; } = "keys/public.pem";
    public string HsKey { get; set; } = "";

    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
