namespace SprintsASP_NetCore_API.Application.Abstractions.Security;


public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
