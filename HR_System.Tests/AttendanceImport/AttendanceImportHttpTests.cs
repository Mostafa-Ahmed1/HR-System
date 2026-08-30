using System.Net;
using HR_System.AttendanceImport;
using HR_System.Controllers;
using HR_System.Models;
using HR_System.Security;
using HR_System.Tests.AttendanceImport;
using HR_System.Tests.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HR_System.Tests.Authentication;

public sealed partial class AuthenticationFlowTests
{
    [Fact]
    public async Task Unauthenticated_attendance_upload_is_challenged()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        using var workbook = ValidAttendanceWorkbook(new DateTime(2026, 9, 1));

        var response = await PostAttendanceAsync(
            client,
            workbook,
            "attendance.xlsx",
            includeAntiforgery: false);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/operation/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task User_without_attendance_add_permission_receives_forbidden()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        await SetPermissionAsync(factory, HrPage.Attendance, CrudOperation.Add, allowed: false);
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);
        using var workbook = ValidAttendanceWorkbook(new DateTime(2026, 9, 2));

        var response = await PostAttendanceAsync(client, workbook, "attendance.xlsx");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
        Assert.Equal(0, await database.Att_dep.CountAsync());
    }

    [Fact]
    public async Task User_with_current_attendance_add_permission_reaches_import_service()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);
        using var workbook = ValidAttendanceWorkbook(new DateTime(2026, 9, 3));

        var response = await PostAttendanceAsync(client, workbook, "attendance.xlsx");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Attendance", response.Headers.Location?.OriginalString);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
        Assert.True(await database.Att_dep.AnyAsync(attendance =>
            attendance.EmpId == AuthenticationWebApplicationFactory.AttendanceEmployeeId
            && attendance.Date == new DateTime(2026, 9, 3)));
    }

    [Fact]
    public async Task Stale_group_claim_cannot_retain_attendance_import_permission()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
            var user = await database.Users.SingleAsync(candidate => candidate.UserId == 7);
            user.GroupId = AuthenticationWebApplicationFactory.DeniedGroupId;
            await database.SaveChangesAsync();
        }

        using var workbook = ValidAttendanceWorkbook(new DateTime(2026, 9, 4));
        var response = await PostAttendanceAsync(client, workbook, "attendance.xlsx");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var verificationScope = factory.Services.CreateScope();
        var verificationDatabase = verificationScope.ServiceProvider.GetRequiredService<HrSysContext>();
        Assert.Equal(0, await verificationDatabase.Att_dep.CountAsync());
    }

    [Fact]
    public async Task Authenticated_upload_without_antiforgery_token_is_rejected()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);
        using var workbook = ValidAttendanceWorkbook(new DateTime(2026, 9, 5));

        var response = await PostAttendanceAsync(
            client,
            workbook,
            "attendance.xlsx",
            includeAntiforgery: false);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Error/400", response.Headers.Location?.OriginalString);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<HrSysContext>();
        Assert.Equal(0, await database.Att_dep.CountAsync());
    }

    [Fact]
    public async Task Unsupported_extension_is_rejected_at_http_boundary()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);
        using var workbook = ValidAttendanceWorkbook(new DateTime(2026, 9, 6));

        var response = await PostAttendanceAsync(client, workbook, "attendance.csv");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Only .xls and .xlsx", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Renamed_non_excel_upload_returns_controlled_validation_feedback()
    {
        await using var factory = new AuthenticationWebApplicationFactory();
        using var client = factory.CreateAuthenticationClient();
        await LoginAsUserAsync(client);
        using var content = new MemoryStream("not an excel workbook"u8.ToArray());

        var response = await PostAttendanceAsync(client, content, "renamed-malware.xlsx");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Import rejected", body);
        Assert.Contains("No attendance records were imported", body);
    }

    [Fact]
    public void Upload_action_keeps_ten_megabyte_framework_request_limit()
    {
        var method = typeof(AttendanceController).GetMethod(nameof(AttendanceController.excelSubmit));
        var limit = Assert.Single(method!.GetCustomAttributes(typeof(RequestSizeLimitAttribute), inherit: true)
            .Cast<RequestSizeLimitAttribute>());

        Assert.Equal(
            AttendanceImportDefaults.MaxUploadBytes,
            ((IRequestSizeLimitMetadata)limit).MaxRequestBodySize);
    }

    private static MemoryStream ValidAttendanceWorkbook(DateTime date)
        => TestWorkbookBuilder.CreateXlsx(
        [
            [
                (double)AuthenticationWebApplicationFactory.AttendanceEmployeeId,
                date.ToOADate(),
                0.375d,
                0.7083333333d
            ]
        ]);

    private static async Task<HttpResponseMessage> PostAttendanceAsync(
        HttpClient client,
        Stream workbook,
        string fileName,
        bool includeAntiforgery = true)
    {
        using var form = new MultipartFormDataContent();
        if (includeAntiforgery)
        {
            var token = await client.GetStringAsync("/__test/auth/antiforgery");
            form.Add(new StringContent(token), "__RequestVerificationToken");
        }

        workbook.Position = 0;
        form.Add(new StreamContent(workbook), "File", fileName);
        return await client.PostAsync("/Attendance/excelSubmit", form);
    }
}
