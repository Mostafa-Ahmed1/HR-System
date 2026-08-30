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
    public const int AllowedGroupId = 42;
    public const int DeniedGroupId = 43;
    public const int TargetUserId = 8;
    public const int EmployeesPageId = 101;
    public const int PermissionsPageId = 207;
    public const int UsersPageId = 97;
    public const int VacationsPageId = 422;
    public const int GeneralSettingsPageId = 509;
    public const int AttendancePageId = 3;
    public const int SalaryPageId = 734;
    public const int AttendanceEmployeeId = 15;

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
            GroupId = AllowedGroupId
        };
        user.Password = new PasswordHasher<User>().HashPassword(user, HashedPassword);

        var targetUser = new User
        {
            UserId = TargetUserId,
            Username = "target-user",
            Email = "target-user@example.invalid",
            GroupId = AllowedGroupId
        };
        targetUser.Password = new PasswordHasher<User>().HashPassword(targetUser, "TargetPassword!1");

        var allowedGroup = new Group { GroupId = AllowedGroupId, GroupName = "Allowed" };
        var deniedGroup = new Group { GroupId = DeniedGroupId, GroupName = "Denied" };
        var attendanceEmployee = new Employee
        {
            EmpId = AttendanceEmployeeId,
            EmpName = "Attendance Employee",
            Address = "Test Address",
            Phone = "01000000000",
            Gender = "Male",
            Nationality = "Egyptian",
            Birthdate = new DateTime(1990, 1, 1),
            NationalId = "12345678901234",
            Hiredate = new DateTime(2020, 1, 1),
            FixedSalary = 10_000,
            AttTime = new TimeSpan(9, 0, 0),
            DepartureTime = new TimeSpan(17, 0, 0)
        };
        var pages = Enum.GetValues<HrPage>()
            .Select(page => new Page
            {
                PageId = GetPageId(page),
                PageName = page.GetBusinessName()
            })
            .ToArray();

        database.AddRange(admin, allowedGroup, deniedGroup, user, targetUser, attendanceEmployee);
        database.Pages.AddRange(pages);
        database.CRUDs.AddRange(Enum.GetValues<HrPage>().SelectMany(page => new[]
        {
            new Crud
            {
                GroupId = AllowedGroupId,
                PageId = GetPageId(page),
                Read = true,
                Add = true,
                Update = true,
                Delete = true
            },
            new Crud
            {
                GroupId = DeniedGroupId,
                PageId = GetPageId(page),
                Read = false,
                Add = false,
                Update = false,
                Delete = false
            }
        }));
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

    public static int GetPageId(HrPage page)
        => page switch
        {
            HrPage.Employees => EmployeesPageId,
            HrPage.Permissions => PermissionsPageId,
            HrPage.Users => UsersPageId,
            HrPage.Vacations => VacationsPageId,
            HrPage.GeneralSettings => GeneralSettingsPageId,
            HrPage.Attendance => AttendancePageId,
            HrPage.Salary => SalaryPageId,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };
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
