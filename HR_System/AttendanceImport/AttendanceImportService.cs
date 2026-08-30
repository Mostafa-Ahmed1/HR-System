using System.Buffers;
using System.Data;
using HR_System.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace HR_System.AttendanceImport;

public interface IAttendanceImportService
{
    Task<AttendanceImportResult> ImportAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default);
}

public sealed class AttendanceImportService : IAttendanceImportService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xls",
        ".xlsx"
    };

    private const int QueryBatchSize = 1_000;
    private readonly HrSysContext _database;
    private readonly IAttendanceWorkbookParser _parser;
    private readonly AttendanceImportOptions _options;
    private readonly ILogger<AttendanceImportService> _logger;

    public AttendanceImportService(
        HrSysContext database,
        IAttendanceWorkbookParser parser,
        IOptions<AttendanceImportOptions> options,
        ILogger<AttendanceImportService> logger)
    {
        _database = database;
        _parser = parser;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AttendanceImportResult> ImportAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var extension = Path.GetExtension(fileName);
        if (!SupportedExtensions.Contains(extension))
        {
            return Reject([new AttendanceImportError(null, "File", "Only .xls and .xlsx attendance files are accepted.")]);
        }

        var bufferedFile = await ReadBoundedAsync(stream, cancellationToken);
        if (bufferedFile is null)
        {
            return Reject([new AttendanceImportError(null, "File", $"The file exceeds the {_options.MaxUploadBytes / (1024 * 1024)} MB upload limit.")]);
        }

        await using (bufferedFile)
        {
            if (bufferedFile.Length == 0)
            {
                return Reject([new AttendanceImportError(null, "File", "Select a non-empty attendance file.")]);
            }

            bufferedFile.Position = 0;
            var parsed = _parser.Parse(bufferedFile, cancellationToken);
            if (!parsed.IsValid)
            {
                return Reject(parsed.Errors, parsed.TotalErrorCount);
            }

            return await ValidateAndPersistAsync(parsed.Rows, cancellationToken);
        }
    }

    private async Task<AttendanceImportResult> ValidateAndPersistAsync(
        IReadOnlyList<AttendanceImportRow> rows,
        CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            if (_database.Database.IsRelational())
            {
                transaction = await _database.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);
            }

            var errors = new List<AttendanceImportError>();
            var validationErrorCount = 0;
            var employeeCodes = rows.Select(row => row.EmployeeCode).Distinct().ToArray();
            var existingEmployeeCodes = new HashSet<int>();

            foreach (var employeeBatch in employeeCodes.Chunk(QueryBatchSize))
            {
                var found = await _database.Employees
                    .AsNoTracking()
                    .Where(employee => employeeBatch.Contains(employee.EmpId))
                    .Select(employee => employee.EmpId)
                    .ToListAsync(cancellationToken);
                existingEmployeeCodes.UnionWith(found);
            }

            foreach (var row in rows.Where(row => !existingEmployeeCodes.Contains(row.EmployeeCode)))
            {
                AddError(errors, ref validationErrorCount, new AttendanceImportError(
                    row.RowNumber,
                    "Employee Code",
                    $"Employee code {row.EmployeeCode} does not exist."));
            }

            var existingAttendance = new HashSet<(int EmployeeCode, DateTime Date)>();
            var minimumDate = rows.Min(row => row.Date);
            var maximumDate = rows.Max(row => row.Date);

            foreach (var employeeBatch in employeeCodes.Chunk(QueryBatchSize))
            {
                var found = await _database.Att_dep
                    .AsNoTracking()
                    .Where(attendance => employeeBatch.Contains(attendance.EmpId)
                                         && attendance.Date >= minimumDate
                                         && attendance.Date <= maximumDate)
                    .Select(attendance => new { attendance.EmpId, attendance.Date })
                    .ToListAsync(cancellationToken);
                existingAttendance.UnionWith(found.Select(item => (item.EmpId, item.Date.Date)));
            }

            foreach (var row in rows.Where(row => existingAttendance.Contains((row.EmployeeCode, row.Date))))
            {
                AddError(errors, ref validationErrorCount, new AttendanceImportError(
                    row.RowNumber,
                    "Duplicate",
                    $"Attendance already exists for employee {row.EmployeeCode} on {row.Date:yyyy-MM-dd}."));
            }

            if (validationErrorCount > 0)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return Reject(errors, validationErrorCount);
            }

            var entities = rows.Select(row => new AttDep
            {
                EmpId = row.EmployeeCode,
                Date = row.Date,
                Attendance = row.Attendance,
                Departure = row.Departure
            }).ToArray();

            _database.Att_dep.AddRange(entities);
            await _database.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Attendance import completed. ImportedRowCount: {ImportedRowCount}",
                entities.Length);
            return AttendanceImportResult.Succeeded(entities.Length);
        }
        catch (DbUpdateException exception)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            _logger.LogError(
                exception,
                "Attendance import persistence failed. CandidateRowCount: {CandidateRowCount}",
                rows.Count);
            return Reject([new AttendanceImportError(
                null,
                "Persistence",
                "The attendance records could not be saved. No records were imported.")]);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task<MemoryStream?> ReadBoundedAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        var destination = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(81_920);

        try
        {
            long totalBytes = 0;
            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                totalBytes += bytesRead;
                if (totalBytes > _options.MaxUploadBytes)
                {
                    await destination.DisposeAsync();
                    return null;
                }

                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            return destination;
        }
        catch
        {
            await destination.DisposeAsync();
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void AddError(
        List<AttendanceImportError> errors,
        ref int totalErrorCount,
        AttendanceImportError error)
    {
        totalErrorCount++;
        if (errors.Count < _options.MaxStoredErrors)
        {
            errors.Add(error);
        }
    }

    private AttendanceImportResult Reject(
        IEnumerable<AttendanceImportError> errors,
        int? totalErrorCount = null)
    {
        var result = AttendanceImportResult.Failed(errors, totalErrorCount);
        _logger.LogWarning(
            "Attendance import rejected. ValidationErrorCount: {ValidationErrorCount}",
            result.TotalErrorCount);
        return result;
    }
}
