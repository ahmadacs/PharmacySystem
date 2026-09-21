namespace Application.Common.Security;

/// <summary>
/// JWT claim types shared by the token issuance side (Infrastructure),
/// validation/consumption side (WebApi) and SignalR hubs so the wire format
/// can never drift apart. Use these instead of string literals.
/// </summary>
public static class JwtClaimTypes
{
    public const string Permission = "permission";
    public const string Role = "role";
}
