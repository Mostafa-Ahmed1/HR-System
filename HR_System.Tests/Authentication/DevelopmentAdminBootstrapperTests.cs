using System.Net;
using System.Text.RegularExpressions;
using HR_System.Models;
using HR_System.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HR_System.Tests.Authentication;

public sealed partial class DevelopmentAdminBootstrapperTests
{
    [Fact]
    public async Task Development_environment_creates_admin_when_missing()
    {
        await using var database = CreateDatabase();
        var bootstrapper = CreateBootstrapper(database, Environments.Development);

        await bootstrapper.EnsureCreatedAsync();

        var admin = await database.Admins.SingleAsync();
        Assert.Equal(DevelopmentAdminDefaults.Username, admin.AdminName);
    }

    [Fact]
    public async Task Development_admin_password_is_stored_as_a_framework_hash()
    {
        await using var database = CreateDatabase();
        var bootstrapper = CreateBootstrapper(database, Environments.Development);

        await bootstrapper.EnsureCreatedAsync();

        var admin = await database.Admins.SingleAsync();
        Assert.NotEqual(DevelopmentAdminDefaults.Password, admin.AdminPass);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<Admin>().VerifyHashedPassword(
                admin,
                admin.AdminPass!,
                DevelopmentAdminDefaults.Password));
    }

    [Fact]
    public async Task Running_bootstrap_twice_does_not_create_duplicate_admins()
    {
        await using var database = CreateDatabase();
        var bootstrapper = CreateBootstrapper(database, Environments.Development);

        await bootstrapper.EnsureCreatedAsync();
        await bootstrapper.EnsureCreatedAsync();

        Assert.Equal(
            1,
            await database.Admins.CountAsync(admin =>
                admin.AdminName == DevelopmentAdminDefaults.Username));
    }

    [Fact]
    public async Task Existing_admin_password_is_not_reset()
    {
        await using var database = CreateDatabase();
        var existing = new Admin
        {
            AdminName = DevelopmentAdminDefaults.Username,
            AdminPass = "existing-password-value"
        };
        database.Admins.Add(existing);
        await database.SaveChangesAsync();

        var bootstrapper = CreateBootstrapper(database, Environments.Development);
        await bootstrapper.EnsureCreatedAsync();

        var admin = await database.Admins.SingleAsync();
        Assert.Equal("existing-password-value", admin.AdminPass);
    }

    [Fact]
    public async Task Non_development_environment_does_not_create_admin()
    {
        await using var database = CreateDatabase();
        var bootstrapper = CreateBootstrapper(database, Environments.Production);

        await bootstrapper.EnsureCreatedAsync();

        Assert.Empty(await database.Admins.ToListAsync());
    }

    [Fact]
    public async Task Development_admin_authenticates_through_existing_login_flow()
    {
        await using var factory = new DevelopmentAdminWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

        var loginPage = await client.GetAsync("/operation/login");
        var loginBody = await loginPage.Content.ReadAsStringAsync();
        var tokenMatch = AntiforgeryTokenRegex().Match(loginBody);

        Assert.Equal(HttpStatusCode.OK, loginPage.StatusCode);
        Assert.True(tokenMatch.Success);

        var login = await client.PostAsync(
            "/operation/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = DevelopmentAdminDefaults.Username,
                ["Password"] = DevelopmentAdminDefaults.Password,
                ["RememberMe"] = "false",
                ["ReturnUrl"] = string.Empty,
                ["__RequestVerificationToken"] = tokenMatch.Groups[1].Value
            }));

        var cookies = login.Headers.GetValuesOrEmpty("Set-Cookie").ToArray();
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/Dashboard", login.Headers.Location?.OriginalString);
        Assert.Contains(
            cookies,
            value => value.StartsWith("__Host-HRSystem.Auth=", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(cookies, value => value.StartsWith("id=", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(cookies, value => value.StartsWith("role=", StringComparison.OrdinalIgnoreCase));

        var dashboard = await client.GetAsync(login.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
    }

    private static HrSysContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<HrSysContext>()
            .UseInMemoryDatabase($"dev-admin-tests-{Guid.NewGuid():N}")
            .Options;
        return new HrSysContext(options);
    }

    private static DevelopmentAdminBootstrapper CreateBootstrapper(
        HrSysContext database,
        string environmentName)
        => new(
            database,
            new PasswordHasher<Admin>(),
            new TestHostEnvironment { EnvironmentName = environmentName },
            NullLogger<DevelopmentAdminBootstrapper>.Instance);

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}

internal sealed class DevelopmentAdminWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"dev-admin-login-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<HrSysContext>();
            services.RemoveAll<DbContextOptions<HrSysContext>>();
            services.AddDbContext<HrSysContext>(options =>
                options.UseLazyLoadingProxies().UseInMemoryDatabase(_databaseName));
        });
    }
}

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "HR_System.Tests";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
