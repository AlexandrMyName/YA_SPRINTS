using Sprints.Bookings.Application.Abstractions.Security;
using System.Security.Cryptography;
using System.Text;


namespace Sprints.Bookings.Infrastructure.Security;


public class PasswordHasher : IPasswordHasher
{

    public string Hash(string password)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    public bool Verify(string password, string hash)
        => string.Equals(Hash(password), hash, StringComparison.OrdinalIgnoreCase);
}
