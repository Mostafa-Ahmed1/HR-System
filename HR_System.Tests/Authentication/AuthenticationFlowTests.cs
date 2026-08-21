using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using HR_System.Models;
using HR_System.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HR_System.Tests.Authentication;

public sealed partial class AuthenticationFlowTests
{
    [Fact]
    public async Task Unauthenticated_access_to_protected_content_redirects_to_login()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();

        var response = await client.GetAsync("/__test/auth/identity");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/operation/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Invalid_login_fails_without_issuing_an_authentication_cookie()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();

        var response = await LoginAsync(client, "nobody", "WrongPassword!1");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Incorrect username or password.", body);
        Assert.DoesNotContain(
            response.Headers.GetValuesOrEmpty("Set-Cookie"),
            value => value.StartsWith("__Host-HRSystem.Auth=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Successful_login_establishes_expected_claims_identity()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();

        var login = await LoginAsync(
            client,
            AuthenticationWebApplicationFactory.HashedUserName,
            AuthenticationWebApplicationFactory.HashedPassword);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var identity = await client.GetFromJsonAsync<IdentityProbe>("/__test/auth/identity");

        Assert.NotNull(identity);
        Assert.True(identity.IsAuthenticated);
        Assert.Equal("7", identity.Id);
        Assert.Equal(AuthenticationWebApplicationFactory.HashedUserName, identity.Name);
        Assert.Equal(HrRoles.User, identity.Role);
        Assert.Equal("42", identity.GroupId);
    }

    [Fact]
    public async Task Remember_me_uses_only_the_secure_framework_authentication_cookie()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();

        var response = await LoginAsync(
            client,
            AuthenticationWebApplicationFactory.HashedUserName,
            AuthenticationWebApplicationFactory.HashedPassword,
            rememberMe: true);
        var cookies = response.Headers.GetValuesOrEmpty("Set-Cookie").ToArray();
        var authenticationCookie = Assert.Single(
            cookies,
            value => value.StartsWith("__Host-HRSystem.Auth=", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("httponly", authenticationCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", authenticationCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", authenticationCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", authenticationCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(cookies, value => value.StartsWith("id=", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(cookies, value => value.StartsWith("role=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Logout_invalidates_authentication()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();

        var login = await LoginAsync(
            client,
            AuthenticationWebApplicationFactory.HashedUserName,
            AuthenticationWebApplicationFactory.HashedPassword);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var antiforgeryToken = await client.GetStringAsync("/__test/auth/antiforgery");
        var logout = await client.PostAsync(
            "/operation/logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgeryToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        var protectedResponse = await client.GetAsync("/__test/auth/identity");
        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);
        Assert.Equal("/operation/login", protectedResponse.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Legacy_plaintext_password_is_migrated_to_a_framework_hash()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();

        var login = await LoginAsync(
            client,
            AuthenticationWebApplicationFactory.LegacyAdminName,
            AuthenticationWebApplicationFactory.LegacyPassword);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
        var migrated = await database.Admins.AsNoTracking().SingleAsync();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordMigrationService<Admin>>();

        Assert.NotEqual(AuthenticationWebApplicationFactory.LegacyPassword, migrated.AdminPass);
        Assert.True(passwords.IsFrameworkHash(migrated.AdminPass));
    }

    [Fact]
    public async Task Migrated_hash_authenticates_on_a_subsequent_login()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using (var firstClient = factory.CreateAuthenticationClient())
        {
            var firstLogin = await LoginAsync(
                firstClient,
                AuthenticationWebApplicationFactory.LegacyAdminName,
                AuthenticationWebApplicationFactory.LegacyPassword);
            Assert.Equal(HttpStatusCode.Redirect, firstLogin.StatusCode);
        }

        using var secondClient = factory.CreateAuthenticationClient();
        var secondLogin = await LoginAsync(
            secondClient,
            AuthenticationWebApplicationFactory.LegacyAdminName,
            AuthenticationWebApplicationFactory.LegacyPassword);
        Assert.Equal(HttpStatusCode.Redirect, secondLogin.StatusCode);

        var identity = await secondClient.GetFromJsonAsync<IdentityProbe>("/__test/auth/identity");
        Assert.NotNull(identity);
        Assert.True(identity.IsAuthenticated);
        Assert.Equal(HrRoles.Admin, identity.Role);
        Assert.Equal("1", identity.Id);
    }

    private static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string username,
        string password,
        bool rememberMe = false)
    {
        var loginPage = await client.GetStringAsync("/operation/login");
        var tokenMatch = AntiforgeryTokenRegex().Match(loginPage);
        Assert.True(tokenMatch.Success, "The login form must contain an antiforgery token.");

        return await client.PostAsync(
            "/operation/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = username,
                ["Password"] = password,
                ["RememberMe"] = rememberMe.ToString(),
                ["ReturnUrl"] = string.Empty,
                ["__RequestVerificationToken"] = tokenMatch.Groups[1].Value
            }));
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}

internal static class HttpHeadersExtensions
{
    public static IEnumerable<string> GetValuesOrEmpty(
        this System.Net.Http.Headers.HttpHeaders headers,
        string name)
        => headers.TryGetValues(name, out var values) ? values : [];
}
