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
- SQL Server for database-backed application workflows

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

## Database configuration

The application reads the SQL Server connection from `ConnectionStrings:hrcon`.
For local development, override it without committing credentials, for example:

```bash
dotnet user-secrets init --project HR_System/HR_System.csproj
dotnet user-secrets set --project HR_System/HR_System.csproj \
  "ConnectionStrings:hrcon" "<local SQL Server connection string>"
```

Environment variable `ConnectionStrings__hrcon` is also supported by ASP.NET Core
configuration. Never commit production credentials.

Migration `20260821130000_ExpandPasswordColumns` expands only `Admin.admin_pass`
and `User.password` to `nvarchar(256)`. Apply reviewed migrations to an approved
database before allowing legacy accounts to sign in so a generated password hash
cannot be truncated.

## Run

```bash
dotnet run --project HR_System/HR_System.csproj
```

Use an HTTPS URL from the launch output. The authentication cookie is intentionally
configured as `Secure`, so browsers do not send it over plain HTTP. Database-free
startup and the login page can be validated without SQL Server; login and HR data
pages require a compatible database.

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
