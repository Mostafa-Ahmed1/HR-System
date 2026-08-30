using System.Security.Claims;

namespace HR_System.Security;

public static class HrRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class HrClaimTypes
{
    public const string GroupId = "urn:hr-system:group-id";
}

public static class ClaimsPrincipalExtensions
{
    public static int? GetAccountId(this ClaimsPrincipal principal)
        => ParseIntClaim(principal, ClaimTypes.NameIdentifier);

    public static int? GetAdminId(this ClaimsPrincipal principal)
        => principal.IsInRole(HrRoles.Admin) ? principal.GetAccountId() : null;

    public static int? GetUserId(this ClaimsPrincipal principal)
        => principal.IsInRole(HrRoles.User) ? principal.GetAccountId() : null;

    // Presentation/backward-compatibility only. Sensitive authorization must load
    // the user's current GroupId from the database through IHrPermissionService.
    public static int? GetGroupId(this ClaimsPrincipal principal)
        => ParseIntClaim(principal, HrClaimTypes.GroupId);

    private static int? ParseIntClaim(ClaimsPrincipal principal, string claimType)
        => int.TryParse(principal.FindFirstValue(claimType), out var value) ? value : null;
}
