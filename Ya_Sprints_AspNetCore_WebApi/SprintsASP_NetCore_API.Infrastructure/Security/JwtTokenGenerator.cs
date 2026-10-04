using SprintsASP_NetCore_API.Application.Abstractions.Security;
using SprintsASP_NetCore_API.Application.Abstractions.Options;
using SprintASP_NetCore_API.Domain.Entities;
using System.IdentityModel.Tokens.Jwt; 
using Microsoft.IdentityModel.Tokens; 
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
 

namespace SprintsASP_NetCore_API.Infrastructure.Security;


public class JwtTokenGenerator : IJwtTokenGenerator
{

    private readonly JwtOptions _options;
    private readonly RsaSecurityKey? _rsaPrivateKey;
    private readonly SymmetricSecurityKey? _hsKey;


    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.Equals(_options.Mode, "HS256", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_options.HsKey) || _options.HsKey.Length < 32)
                throw new InvalidOperationException("Jwt:HsKey must be at least 32 chars.");

            _hsKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.HsKey));
        }
        else
        {
            var path = Path.Combine(AppContext.BaseDirectory, _options.PrivateKeyPath);
            _rsaPrivateKey = RsaKeyLoader.LoadPrivateKey(path);
        }
    }


    public AccessTokenResult GenerateAccessToken(User user)
    {
        SigningCredentials creds = _hsKey != null
            ? new SigningCredentials(_hsKey, SecurityAlgorithms.HmacSha256)
            : new SigningCredentials(_rsaPrivateKey!, SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim("userId", user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.Login),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
