# HR System — ASP.NET Core Modernization

HR System is an existing ASP.NET Core MVC application for employee, attendance, vacation, salary, user, group, and permission administration. The project is being modernized incrementally: improving authentication, data access, security, and test coverage while retaining the established MVC application, SQL Server schema, and existing behavior wherever possible.

## What This Project Demonstrates

- Working within an existing ASP.NET Core application
- Modernizing focused areas without an unnecessary rewrite
- Updating authentication while keeping legacy accounts usable
- Using Entity Framework Core with an established SQL Server schema
- Applying targeted security improvements to affected workflows
- Adding automated integration tests around authentication and authorization
- Preserving current application behavior while making small, reviewable changes

## Technology Stack

- C# and .NET 10
- ASP.NET Core MVC
- Entity Framework Core 10 with SQL Server
- Razor views
- Existing Bootstrap and jQuery frontend
- ASP.NET Core cookie authentication
- `PasswordHasher<TUser>`
- xUnit integration tests

## Selected Improvements

The current modernization work focuses on bounded changes that can be reviewed and tested independently:

- Framework-managed authentication and claims
- Password-hash migration support for legacy accounts
- Safer authentication-cookie configuration
- Targeted antiforgery and destructive-action changes in touched workflows
- A reviewed EF Core migration for longer password-hash storage
- Integration coverage for authentication and authorization behavior

This is an incremental modernization, not a complete security redesign or architectural rewrite.

## Authentication Modernization

- ASP.NET Core cookie authentication now represents signed-in users with minimal ID, name, role, and optional group claims.
- Existing plaintext passwords can be upgraded to framework password hashes after a successful legacy comparison.
- Already-hashed passwords use framework verification and can be rehashed when the framework recommends it.
- Persistent sign-in is handled through ASP.NET Core authentication properties.
- Redirects after login accept only local return URLs.

## Security Improvements

- The authentication cookie is configured as HTTP-only, secure, same-site, and host-scoped.
- Login and logout POST actions use antiforgery validation.
- Stored password values are not rendered by the touched profile and user-edit views.
- Touched destructive actions use POST rather than state-changing links.

These changes cover selected workflows only; untouched areas still require their own review.

## Database and EF Core

The application uses Entity Framework Core with SQL Server and the existing `HrSysContext` model.

Migration `20260821130000_ExpandPasswordColumns` expands only `Admin.admin_pass` and `User.password` to `nvarchar(256)`. Apply reviewed migrations to an approved database before allowing legacy accounts to sign in so generated password hashes cannot be truncated.

## Automated Tests

The `HR_System.Tests` project uses xUnit and `Microsoft.AspNetCore.Mvc.Testing` to exercise authentication and authorization flows.

The tests replace SQL Server with the EF Core in-memory provider, so they validate application behavior without claiming SQL Server provider parity. No production database is required to restore, build, or run the automated tests.

## Build and Test

Prerequisite: .NET 10 SDK.

From the repository root:

```bash
dotnet restore HR_System.sln
dotnet build HR_System.sln --configuration Release --no-restore
dotnet test HR_System.sln --configuration Release --no-build
```

Run the application with:

```bash
dotnet run --project HR_System/HR_System.csproj
```

Use an HTTPS URL from the launch output. The authentication cookie is configured as `Secure`, so browsers do not send it over plain HTTP. Database-free startup and the login page can be validated without SQL Server; login and HR data pages require a compatible database.

## Local Database Configuration

The application reads its SQL Server connection string from `ConnectionStrings:hrcon`.

For local development, configure it without committing credentials:

```bash
dotnet user-secrets init --project HR_System/HR_System.csproj
dotnet user-secrets set --project HR_System/HR_System.csproj \
  "ConnectionStrings:hrcon" "<local SQL Server connection string>"
```

The ASP.NET Core environment variable `ConnectionStrings__hrcon` is also supported. Never commit production credentials.

## Development Approach

**Inspect → understand → make the smallest correct change → test → preserve existing behavior.**

The goal is controlled modernization: improve one well-defined area at a time, verify the result, and avoid expanding scope into an unnecessary rewrite.
