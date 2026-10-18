

namespace Sprints.Bookings.Presentation.Extentions;


public static class AuthSchemes
{
    public const string RS256 = "Bearer";
    public const string HS256 = "BearerDev";
}

public static class AuthPolicies
{
    public const string AnyAuthenticated = "AnyAuthenticated";
    public const string Admin = "Admin"; 
}
