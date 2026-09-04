# HR System

HR System is an ASP.NET Core MVC application for employee, attendance, vacation,
salary, user, group, and permission administration. This modernization keeps the
existing MVC application and SQL Server schema while moving the supported runtime
and authentication foundation forward incrementally.

## Technology stack

- .NET 10 and ASP.NET Core MVC
- Entity Framework Core 10 with SQL Server
- Razor views and the existing Bootstrap/jQuery-based frontend
- ASP.NET Core cookie authentication and `PasswordHasher<TUser>`
- xUnit integration tests

## Prerequisites

- .NET 10 SDK
- Visual Studio on Windows with SQL Server Express LocalDB

No production database is needed to restore, build, or run the automated tests.
The authentication tests use the EF Core in-memory provider and do not claim SQL
Server behavioral parity.

## Restore, build, and test

From the repository root:

```bash
dotnet restore HR_System.sln
dotnet build HR_System.sln --configuration Release --no-restore
dotnet test HR_System.sln --configuration Release --no-build
```

## Windows local development

The Development environment overrides `ConnectionStrings:hrcon` with SQL Server
LocalDB and uses the database `hrSystemCF1`. The default connection in
`appsettings.json` is unchanged and is not used by this Development override.

From the repository root, apply the existing migrations, trust the ASP.NET Core
development certificate, and run the project profile:

```bash
dotnet ef database update --project HR_System/HR_System.csproj --startup-project HR_System/HR_System.csproj
dotnet dev-certs https --trust
dotnet run --project HR_System/HR_System.csproj
```

The `HR_System` profile in `launchSettings.json` expects:

- `https://localhost:7017`
- `http://localhost:5017`

The URLs printed by the runtime as `Now listening on:` are authoritative if they
differ from the profile values.

Development startup creates the local `admin` account only when it is missing.
Its password is hashed before storage, and an existing account is never reset.
No Development account is bootstrapped in Production.

Migration `20260821130000_ExpandPasswordColumns` expands only `Admin.admin_pass`
and `User.password` to `nvarchar(256)`. Apply reviewed migrations to an approved
database before allowing legacy accounts to sign in so a generated password hash
cannot be truncated.

Use the HTTPS URL from the launch output. The authentication cookie is intentionally
configured as `Secure`, so browsers do not send it over plain HTTP. Development
startup and login require the migrated LocalDB database because the Development
administrator bootstrap checks the `Admin` table during startup.

## Authentication modernization status

- Framework cookie authentication replaces the legacy client-controlled `id` and
  `role` cookies.
- Authentication identity is represented by minimal ID, name, role, and optional
  group claims.
- Existing plaintext passwords are upgraded to framework password hashes after a
  successful legacy comparison. Already-hashed passwords use framework verification
  and rehash recommendations.
- Stored password values are no longer rendered by the profile or user-edit views.
- Touched state-changing actions use antiforgery protection, and touched destructive
  links use POST actions.

## Remaining modernization roadmap

- Implementation Brief #02: secure the attendance Excel import end to end, including
  authorization policy, resilient row validation, resource controls, transaction
  behavior, and dedicated tests.
- Complete the CSRF and destructive-GET audit across untouched controller actions.
- Review and harden the existing group/page/CRUD permission engine.
- Reduce existing nullable warnings and continue targeted async EF Core conversion.
- Add SQL Server integration coverage for provider-specific computed SQL and critical
  database workflows.

This remains an incremental modernization; it is not a Clean Architecture,
microservices, CQRS, frontend, API, or database redesign.
