using System.Data;
using HR_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HR_System.Security;

public static class DevelopmentAdminDefaults
{
    public const string Username = "admin";
    public const string Password = "Admin@123456";
}

public interface IDevelopmentAdminBootstrapper
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}

public sealed class DevelopmentAdminBootstrapper : IDevelopmentAdminBootstrapper
{
    private readonly HrSysContext _database;
    private readonly IPasswordHasher<Admin> _passwordHasher;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevelopmentAdminBootstrapper> _logger;

    public DevelopmentAdminBootstrapper(
        HrSysContext database,
        IPasswordHasher<Admin> passwordHasher,
        IHostEnvironment environment,
        ILogger<DevelopmentAdminBootstrapper> logger)
    {
        _database = database;
        _passwordHasher = passwordHasher;
        _environment = environment;
        _logger = logger;
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            return;
        }

        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);

        var existingCount = await _database.Admins
            .CountAsync(admin => admin.AdminName == DevelopmentAdminDefaults.Username, cancellationToken);

        if (existingCount > 0)
        {
            if (existingCount > 1)
            {
                _logger.LogWarning(
                    "Development admin bootstrap found {AdminCount} matching accounts and made no changes.",
                    existingCount);
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return;
        }

        var admin = new Admin { AdminName = DevelopmentAdminDefaults.Username };
        admin.AdminPass = _passwordHasher.HashPassword(admin, DevelopmentAdminDefaults.Password);

        _database.Admins.Add(admin);
        await _database.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        _logger.LogInformation("Development admin bootstrap created the local administrator account.");
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(
        CancellationToken cancellationToken)
    {
        if (!_database.Database.IsRelational())
        {
            return null;
        }

        return await _database.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
    }
}
