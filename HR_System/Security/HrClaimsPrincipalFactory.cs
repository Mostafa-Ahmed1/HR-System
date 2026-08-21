using System.Security.Claims;
using HR_System.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HR_System.Security;

public sealed class HrClaimsPrincipalFactory
{
    public ClaimsPrincipal Create(Admin admin)
    {
        ArgumentNullException.ThrowIfNull(admin);

        return CreatePrincipal(
            admin.AdminId,
            admin.AdminName ?? string.Empty,
            HrRoles.Admin,
            groupId: null);
    }

    public ClaimsPrincipal Create(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return CreatePrincipal(
            user.UserId,
            user.Username,
            HrRoles.User,
            user.GroupId);
    }

    private static ClaimsPrincipal CreatePrincipal(int id, string name, string role, int? groupId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role)
        };

        if (groupId.HasValue)
        {
            claims.Add(new Claim(HrClaimTypes.GroupId, groupId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }
}
