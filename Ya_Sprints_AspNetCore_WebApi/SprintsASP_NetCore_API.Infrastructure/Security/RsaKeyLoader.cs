using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography; 


namespace SprintsASP_NetCore_API.Infrastructure.Security;


public static class RsaKeyLoader
{

    public static RsaSecurityKey LoadPrivateKey(string path)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return new RsaSecurityKey(rsa);
    }

    public static RsaSecurityKey LoadPublicKey(string path)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return new RsaSecurityKey(rsa);
    }
}
