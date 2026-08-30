using Microsoft.AspNetCore.Http;

namespace HR_System.AttendanceImport;

public static class AttendanceImportDefaults
{
    public const long MaxUploadBytes = 10 * 1024 * 1024;
    public const int MaxWorksheetRows = 50_000;
    public const int MaxStoredErrors = 100;
    public const long MaxUncompressedOpenXmlBytes = 100 * 1024 * 1024;
}

public sealed class AttendanceImportOptions
{
    public long MaxUploadBytes { get; set; } = AttendanceImportDefaults.MaxUploadBytes;
    public int MaxWorksheetRows { get; set; } = AttendanceImportDefaults.MaxWorksheetRows;
    public int MaxStoredErrors { get; set; } = AttendanceImportDefaults.MaxStoredErrors;
    public long MaxUncompressedOpenXmlBytes { get; set; } = AttendanceImportDefaults.MaxUncompressedOpenXmlBytes;
}

public static class AttendanceImportSchema
{
    public const int EmployeeCodeColumn = 0;
    public const int DateColumn = 1;
    public const int AttendanceColumn = 2;
    public const int DepartureColumn = 3;
    public const int ColumnCount = 4;

    public static readonly string[] FieldNames =
    [
        "Employee Code",
        "Date",
        "Attendance",
        "Departure"
    ];
}

public sealed class AttendanceImportRequest
{
    public IFormFile? File { get; set; }
}

public sealed record AttendanceImportError(int? RowNumber, string Field, string Message);

public sealed class AttendanceImportResult
{
    private AttendanceImportResult(
        bool success,
        int importedCount,
        int totalErrorCount,
        IReadOnlyList<AttendanceImportError> errors)
    {
        Success = success;
        ImportedCount = importedCount;
        TotalErrorCount = totalErrorCount;
        Errors = errors;
    }

    public bool Success { get; }
    public int ImportedCount { get; }
    public int TotalErrorCount { get; }
    public IReadOnlyList<AttendanceImportError> Errors { get; }

    public static AttendanceImportResult Succeeded(int importedCount)
        => new(true, importedCount, 0, []);

    public static AttendanceImportResult Failed(
        IEnumerable<AttendanceImportError> errors,
        int? totalErrorCount = null)
    {
        var materialized = errors.ToArray();
        return new(false, 0, totalErrorCount ?? materialized.Length, materialized);
    }
}

public sealed record AttendanceImportRow(
    int RowNumber,
    int EmployeeCode,
    DateTime Date,
    TimeSpan Attendance,
    TimeSpan Departure);

public sealed record AttendanceWorkbookParseResult(
    IReadOnlyList<AttendanceImportRow> Rows,
    IReadOnlyList<AttendanceImportError> Errors,
    int TotalErrorCount)
{
    public bool IsValid => TotalErrorCount == 0;
}
