using System.Net;
using System.Net.Http.Json;
using HR_System.Models;
using HR_System.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HR_System.Tests.Authentication;

public sealed partial class AuthenticationFlowTests
{
    [Fact]
    public async Task Unauthenticated_user_cannot_invoke_users_read_endpoint()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();

        var response = await client.GetAsync("/User/allusers");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/operation/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task User_with_read_denied_cannot_invoke_users_read_endpoint()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        await SetPermissionAsync(factory, HrPage.Users, CrudOperation.Read, allowed: false);
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);

        var response = await client.GetAsync("/User/allusers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Users_add_does_not_use_attendance_permission_at_the_previous_users_page_id()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        await SetPermissionAsync(factory, HrPage.Users, CrudOperation.Add, allowed: false);
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
            var enumOrdinalDecoyId = (int)HrPage.Users;
            database.Pages.Add(new Page
            {
                PageId = enumOrdinalDecoyId,
                PageName = "Enum Ordinal Decoy"
            });
            database.CRUDs.Add(new Crud
            {
                GroupId = AuthenticationWebApplicationFactory.AllowedGroupId,
                PageId = enumOrdinalDecoyId,
                Read = true,
                Add = true,
                Update = true,
                Delete = true
            });
            await database.SaveChangesAsync();

            var attendanceAtOldUsersId = await database.Pages.AsNoTracking().SingleAsync(page =>
                page.PageId == 3);
            var usersAtUnrelatedId = await database.Pages.AsNoTracking().SingleAsync(page =>
                page.PageName == HrPage.Users.GetBusinessName());
            var attendanceAdd = await database.CRUDs.AsNoTracking().SingleAsync(crud =>
                crud.GroupId == AuthenticationWebApplicationFactory.AllowedGroupId
                && crud.PageId == attendanceAtOldUsersId.PageId);
            var usersAdd = await database.CRUDs.AsNoTracking().SingleAsync(crud =>
                crud.GroupId == AuthenticationWebApplicationFactory.AllowedGroupId
                && crud.PageId == usersAtUnrelatedId.PageId);
            var enumOrdinalDecoyAdd = await database.CRUDs.AsNoTracking().SingleAsync(crud =>
                crud.GroupId == AuthenticationWebApplicationFactory.AllowedGroupId
                && crud.PageId == enumOrdinalDecoyId);

            Assert.Equal("Attendance", attendanceAtOldUsersId.PageName);
            Assert.Equal(AuthenticationWebApplicationFactory.UsersPageId, usersAtUnrelatedId.PageId);
            Assert.True(attendanceAdd.Add);
            Assert.False(usersAdd.Add);
            Assert.True(enumOrdinalDecoyAdd.Add);
        }

        var response = await PostUserAsync(client, "/User/addUser", NewUserForm("blocked-add"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_with_update_denied_cannot_invoke_users_edit_post_directly()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        await SetPermissionAsync(factory, HrPage.Users, CrudOperation.Update, allowed: false);
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);

        var response = await PostUserAsync(
            client,
            "/User/edit",
            new Dictionary<string, string>
            {
                ["UserId"] = AuthenticationWebApplicationFactory.TargetUserId.ToString(),
                ["Username"] = "blocked-edit",
                ["Email"] = "blocked-edit@example.invalid",
                ["GroupId"] = AuthenticationWebApplicationFactory.AllowedGroupId.ToString()
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_with_delete_denied_cannot_invoke_users_delete_post_directly()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        await SetPermissionAsync(factory, HrPage.Users, CrudOperation.Delete, allowed: false);
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);

        var response = await PostUserAsync(
            client,
            $"/User/delete?id={AuthenticationWebApplicationFactory.TargetUserId}",
            []);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Users_authorization_succeeds_when_users_page_id_is_unrelated_to_enum_order()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);

        Assert.Equal(97, AuthenticationWebApplicationFactory.UsersPageId);
        Assert.NotEqual(3, AuthenticationWebApplicationFactory.UsersPageId);

        var response = await PostUserAsync(client, "/User/addUser", NewUserForm("allowed-add"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
        Assert.True(await database.Users.AsNoTracking().AnyAsync(user => user.Username == "allowed-add"));
    }

    [Fact]
    public async Task Current_admin_can_invoke_users_add_post()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        var login = await LoginAsync(
            client,
            AuthenticationWebApplicationFactory.LegacyAdminName,
            AuthenticationWebApplicationFactory.LegacyPassword);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var response = await PostUserAsync(client, "/User/addUser", NewUserForm("admin-add"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
        Assert.True(await database.Users.AsNoTracking().AnyAsync(user => user.Username == "admin-add"));
    }

    [Fact]
    public async Task Missing_crud_rule_fails_closed()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var setupScope = factory.Services.CreateScope();
        var setupDatabase = setupScope.ServiceProvider.GetRequiredService<HrSysContext>();
        var rule = await setupDatabase.CRUDs.SingleAsync(crud =>
            crud.GroupId == AuthenticationWebApplicationFactory.AllowedGroupId
            && crud.PageId == AuthenticationWebApplicationFactory.UsersPageId);
        setupDatabase.CRUDs.Remove(rule);
        await setupDatabase.SaveChangesAsync();

        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);
        var response = await client.GetAsync("/User/allusers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Missing_expected_page_name_fails_closed()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using (var setupScope = factory.Services.CreateScope())
        {
            var database = setupScope.ServiceProvider.GetRequiredService<HrSysContext>();
            var usersPage = await database.Pages.SingleAsync(page =>
                page.PageName == HrPage.Users.GetBusinessName());
            usersPage.PageName = "Renamed Users";
            await database.SaveChangesAsync();
        }

        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);
        var response = await client.GetAsync("/User/allusers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_expected_page_name_fails_closed_even_without_a_second_crud_rule()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using (var setupScope = factory.Services.CreateScope())
        {
            var database = setupScope.ServiceProvider.GetRequiredService<HrSysContext>();
            database.Pages.Add(new Page
            {
                PageId = 997,
                PageName = HrPage.Users.GetBusinessName()
            });
            await database.SaveChangesAsync();
        }

        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);
        var response = await client.GetAsync("/User/allusers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_crud_rule_for_the_expected_page_fails_closed()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using (var setupScope = factory.Services.CreateScope())
        {
            var database = setupScope.ServiceProvider.GetRequiredService<HrSysContext>();
            database.CRUDs.Add(new Crud
            {
                GroupId = AuthenticationWebApplicationFactory.AllowedGroupId,
                PageId = AuthenticationWebApplicationFactory.UsersPageId,
                Read = true,
                Add = true,
                Update = true,
                Delete = true
            });
            await database.SaveChangesAsync();
        }

        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);
        var response = await client.GetAsync("/User/allusers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Deleted_user_fails_closed_with_the_same_authentication_cookie()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
            var user = await database.Users.SingleAsync(candidate =>
                candidate.UserId == 7);
            database.Users.Remove(user);
            await database.SaveChangesAsync();
        }

        var response = await client.GetAsync("/User/allusers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Changing_group_after_login_immediately_revokes_permission_for_the_same_cookie()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);

        var permitted = await client.GetAsync("/User/allusers");
        Assert.Equal(HttpStatusCode.OK, permitted.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
            var user = await database.Users.SingleAsync(candidate => candidate.UserId == 7);
            user.GroupId = AuthenticationWebApplicationFactory.DeniedGroupId;
            await database.SaveChangesAsync();
        }

        var denied = await client.GetAsync("/User/allusers");
        var stillStaleIdentity = await client.GetFromJsonAsync<IdentityProbe>("/__test/auth/identity");

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(
            AuthenticationWebApplicationFactory.AllowedGroupId.ToString(),
            stillStaleIdentity?.GroupId);
    }

    [Fact]
    public async Task Changing_crud_after_login_immediately_revokes_permission_for_the_same_cookie()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);

        var permitted = await client.GetAsync("/User/allusers");
        Assert.Equal(HttpStatusCode.OK, permitted.StatusCode);

        await SetPermissionAsync(factory, HrPage.Users, CrudOperation.Read, allowed: false);

        var denied = await client.GetAsync("/User/allusers");

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task User_without_permissions_add_cannot_create_group_directly()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        await SetPermissionAsync(factory, HrPage.Permissions, CrudOperation.Add, allowed: false);
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);

        var response = await PostUserAsync(
            client,
            "/Group/CreateGroup",
            new Dictionary<string, string> { ["group.GroupName"] = "Blocked Group" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task LoginAsUserAsync(HttpClient client)
    {
        var login = await LoginAsync(
            client,
            AuthenticationWebApplicationFactory.HashedUserName,
            AuthenticationWebApplicationFactory.HashedPassword);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
    }

    private static Dictionary<string, string> NewUserForm(string username)
        => new()
        {
            ["Username"] = username,
            ["Email"] = $"{username}@example.invalid",
            ["Password"] = "NewPassword!1",
            ["GroupId"] = AuthenticationWebApplicationFactory.AllowedGroupId.ToString()
        };

    private static async Task<HttpResponseMessage> PostUserAsync(
        HttpClient client,
        string path,
        Dictionary<string, string> values)
    {
        var antiforgeryToken = await client.GetStringAsync("/__test/auth/antiforgery");
        values["__RequestVerificationToken"] = antiforgeryToken;
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }

    private static async Task SetPermissionAsync(
        AuthenticationWebApplicationFactory factory,
        HrPage page,
        CrudOperation operation,
        bool allowed)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
        var rule = await database.CRUDs.SingleAsync(crud =>
            crud.GroupId == AuthenticationWebApplicationFactory.AllowedGroupId
            && crud.PageId == AuthenticationWebApplicationFactory.GetPageId(page));

        switch (operation)
        {
            case CrudOperation.Read:
                rule.Read = allowed;
                break;
            case CrudOperation.Add:
                rule.Add = allowed;
                break;
            case CrudOperation.Update:
                rule.Update = allowed;
                break;
            case CrudOperation.Delete:
                rule.Delete = allowed;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
        }

        await database.SaveChangesAsync();
    }
}
