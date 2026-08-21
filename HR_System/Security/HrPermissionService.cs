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

// These values mirror the existing PageId mapping used by the navigation and
// permission-management UI. They are not a new authorization taxonomy.
public enum HrPage
{
    Employees = 1,
    Permissions = 2,
    Users = 3,
    Vacations = 4,
    GeneralSettings = 5,
    Attendance = 6,
    Salary = 7
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

        // Deliberately do not use the GroupId cookie claim here. The joins resolve
        // the user's current group and permission record on every authorization check.
        var matches = await (
                from user in database.Users.AsNoTracking()
                join permissionGroup in database.Groups.AsNoTracking()
                    on user.GroupId equals (int?)permissionGroup.GroupId
                join crud in database.CRUDs.AsNoTracking()
                    on permissionGroup.GroupId equals crud.GroupId
                join permissionPage in database.Pages.AsNoTracking()
                    on crud.PageId equals permissionPage.PageId
                where user.UserId == accountId.Value
                    && permissionPage.PageId == (int)page
                select new
                {
                    permissionPage.PageName,
                    crud.Read,
                    crud.Add,
                    crud.Update,
                    crud.Delete
                })
            .Take(2)
            .ToListAsync(cancellationToken);

        // Missing, duplicate, or malformed page/CRUD state is ambiguous and must deny.
        if (matches.Count != 1 || string.IsNullOrWhiteSpace(matches[0].PageName))
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
