using System.Buffers.Binary;
using Microsoft.AspNetCore.Identity;

namespace HR_System.Security;

public enum PasswordCheckResult
{
    Failed,
    Succeeded,
    SucceededRehashNeeded,
    SucceededLegacyUpgradeNeeded
}

public interface IPasswordMigrationService<TAccount> where TAccount : class
{
    PasswordCheckResult Verify(TAccount account, string? storedPassword, string providedPassword);
    string Hash(TAccount account, string password);
    bool IsFrameworkHash(string? storedPassword);
}

public sealed class PasswordMigrationService<TAccount> : IPasswordMigrationService<TAccount>
    where TAccount : class
{
    private readonly IPasswordHasher<TAccount> _passwordHasher;

    public PasswordMigrationService(IPasswordHasher<TAccount> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public PasswordCheckResult Verify(TAccount account, string? storedPassword, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(providedPassword);

        if (string.IsNullOrEmpty(storedPassword))
        {
            return PasswordCheckResult.Failed;
        }

        if (!IsFrameworkHash(storedPassword))
        {
            return string.Equals(storedPassword, providedPassword, StringComparison.Ordinal)
                ? PasswordCheckResult.SucceededLegacyUpgradeNeeded
                : PasswordCheckResult.Failed;
        }

        var result = _passwordHasher.VerifyHashedPassword(account, storedPassword, providedPassword);
        return result switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Succeeded,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SucceededRehashNeeded,
            _ => PasswordCheckResult.Failed
        };
    }

    public string Hash(TAccount account, string password)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(password);
        return _passwordHasher.HashPassword(account, password);
    }

    public bool IsFrameworkHash(string? storedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword))
        {
            return false;
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(storedPassword);
        }
        catch (FormatException)
        {
            return false;
        }

        if (payload.Length == 49 && payload[0] == 0x00)
        {
            return true;
        }

        if (payload.Length < 29 || payload[0] != 0x01)
        {
            return false;
        }

        var iterationCount = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(5, 4));
        var saltLength = BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(9, 4));

        return iterationCount > 0
            && saltLength >= 16
            && saltLength <= int.MaxValue
            && payload.Length >= 13 + (int)saltLength + 16;
    }
}
