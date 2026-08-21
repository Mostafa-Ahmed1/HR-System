using System.Security.Claims;
using HR_System.Models;
using HR_System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HR_System.Tests.Authentication;

public sealed class AuthenticationWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string LegacyAdminName = "legacy-admin";
    public const string LegacyPassword = "LegacyPassword!1";
    public const string HashedUserName = "hashed-user";
    public const string HashedPassword = "HashedPassword!1";

    private readonly string _databaseName = $"hr-auth-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<HrSysContext>();
            services.RemoveAll<DbContextOptions<HrSysContext>>();
            services.AddDbContext<HrSysContext>(options =>
                options.UseLazyLoadingProxies().UseInMemoryDatabase(_databaseName));

            services.AddControllers().AddApplicationPart(typeof(IdentityProbeController).Assembly);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();

        var admin = new Admin
        {
            AdminId = 1,
            AdminName = LegacyAdminName,
            AdminPass = LegacyPassword
        };

        var user = new User
        {
            UserId = 7,
            Username = HashedUserName,
            Email = "hashed-user@example.invalid",
            GroupId = 42
        };
        user.Password = new PasswordHasher<User>().HashPassword(user, HashedPassword);

        database.AddRange(admin, user);
        database.SaveChanges();

        return host;
    }

    public HttpClient CreateAuthenticationClient(bool handleCookies = true)
        => CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = handleCookies
        });
}

[Route("__test/auth")]
public sealed class IdentityProbeController : ControllerBase
{
    [Authorize]
    [HttpGet("identity")]
    public IActionResult Identity()
        => Ok(new IdentityProbe(
            User.Identity?.IsAuthenticated == true,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.Identity?.Name,
            User.FindFirstValue(ClaimTypes.Role),
            User.FindFirstValue(HrClaimTypes.GroupId)));

    [Authorize]
    [HttpGet("antiforgery")]
    public IActionResult Antiforgery([FromServices] IAntiforgery antiforgery)
        => Content(antiforgery.GetAndStoreTokens(HttpContext).RequestToken!);
}

public sealed record IdentityProbe(
    bool IsAuthenticated,
    string? Id,
    string? Name,
    string? Role,
    string? GroupId);
