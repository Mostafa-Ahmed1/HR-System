using System.Security.Claims;
using HR_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Security;

public enum CrudOperation
{
    Read,
    Add,
    Update,
    Delete
}

public enum HrPage
{
    Employees,
    Permissions,
    Users,
    Vacations,
    GeneralSettings,
    Attendance,
    Salary
}

public static class HrPageExtensions
{
    public static string? GetBusinessName(this HrPage page)
        => page switch
        {
            HrPage.Employees => "Employees",
            HrPage.Permissions => "Permissions",
            HrPage.Users => "Users",
            HrPage.Vacations => "Vacations",
            HrPage.GeneralSettings => "General Settings",
            HrPage.Attendance => "Attendance",
            HrPage.Salary => "Salary",
            _ => null
        };
}

public interface IHrPermissionService
{
    Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        HrPage page,
        CrudOperation operation,
        CancellationToken cancellationToken = default);
}

public sealed class HrPermissionService(HrSysContext database) : IHrPermissionService
{
    public async Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        HrPage page,
        CrudOperation operation,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var expectedPageName = page.GetBusinessName();
        if (expectedPageName is null || !Enum.IsDefined(typeof(CrudOperation), operation))
        {
            return false;
        }

        var accountId = principal.GetAccountId();
        if (accountId is null)
        {
            return false;
        }

        if (principal.IsInRole(HrRoles.Admin))
        {
            // A deleted Admin must not retain access through a persistent cookie.
            return await database.Admins
                .AsNoTracking()
                .AnyAsync(admin => admin.AdminId == accountId.Value, cancellationToken);
        }

        if (!principal.IsInRole(HrRoles.User))
        {
            return false;
        }

        // Deliberately do not use the GroupId cookie claim or a fixed PageId here.
        // The joins resolve the current user, group, business page, and permission
        // record on every authorization check.
        var matches = await (
                from user in database.Users.AsNoTracking()
                join permissionGroup in database.Groups.AsNoTracking()
                    on user.GroupId equals (int?)permissionGroup.GroupId
                join crud in database.CRUDs.AsNoTracking()
                    on permissionGroup.GroupId equals crud.GroupId
                join permissionPage in database.Pages.AsNoTracking()
                    on crud.PageId equals permissionPage.PageId
                where user.UserId == accountId.Value
                    && permissionPage.PageName == expectedPageName
                select new
                {
                    MatchingPageCount = database.Pages.Count(candidatePage =>
                        candidatePage.PageName == expectedPageName),
                    crud.Read,
                    crud.Add,
                    crud.Update,
                    crud.Delete
                })
            .Take(2)
            .ToListAsync(cancellationToken);

        // Exactly one business Page and one CRUD row must participate. Missing or
        // duplicate data is ambiguous and therefore denied.
        if (matches.Count != 1 || matches[0].MatchingPageCount != 1)
        {
            return false;
        }

        return operation switch
        {
            CrudOperation.Read => matches[0].Read,
            CrudOperation.Add => matches[0].Add,
            CrudOperation.Update => matches[0].Update,
            CrudOperation.Delete => matches[0].Delete,
            _ => false
        };
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HrPermissionAttribute : TypeFilterAttribute
{
    public HrPermissionAttribute(HrPage page, CrudOperation operation)
        : base(typeof(HrPermissionFilter))
    {
        Arguments = [page, operation];
    }
}

public sealed class HrPermissionFilter(
    IHrPermissionService permissions,
    HrPage page,
    CrudOperation operation) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        if (!await permissions.HasPermissionAsync(
                context.HttpContext.User,
                page,
                operation,
                context.HttpContext.RequestAborted))
        {
            context.Result = new ForbidResult();
        }
    }
}
