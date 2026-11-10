using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class JwtSettings
{
    public const string Issuer = "FoodOrderApi";
    public const string Audience = "FoodOrderClients";

    // The fallback key is for local development only. Set Jwt:Key (user secrets or an environment variable) for anything real.
    public static SymmetricSecurityKey GetKey(IConfiguration config)
    {
        string secret = config["Jwt:Key"] ?? "dev-only-key-change-me-use-at-least-32-characters";
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }
}