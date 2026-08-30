using System.Globalization;
using System.IO.Compression;
using System.Text;
using ExcelDataReader;
using ExcelDataReader.Exceptions;
using Microsoft.Extensions.Options;

namespace HR_System.AttendanceImport;

public interface IAttendanceWorkbookParser
{
    AttendanceWorkbookParseResult Parse(Stream stream, CancellationToken cancellationToken = default);
}

public sealed class AttendanceWorkbookParser : IAttendanceWorkbookParser
{
    private readonly AttendanceImportOptions _options;

    public AttendanceWorkbookParser(IOptions<AttendanceImportOptions> options)
    {
        _options = options.Value;
    }

    public AttendanceWorkbookParseResult Parse(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        try
        {
            var containerError = ValidateOpenXmlContainer(stream);
            if (containerError is not null)
            {
                return containerError;
            }

            using var reader = ExcelReaderFactory.CreateReader(
                stream,
                new ExcelReaderConfiguration
                {
                    LeaveOpen = true,
                    FallbackEncoding = Encoding.GetEncoding(1252)
                });

            return ParseFirstWorksheet(reader, cancellationToken);
        }
        catch (HeaderException)
        {
            return InvalidWorkbook();
        }
        catch (InvalidPasswordException)
        {
            return InvalidWorkbook("Password-protected workbooks are not supported.");
        }
        catch (InvalidDataException)
        {
            return InvalidWorkbook();
        }
        catch (IOException)
        {
            return InvalidWorkbook();
        }
        catch (NotSupportedException)
        {
            return InvalidWorkbook();
        }
    }

    private AttendanceWorkbookParseResult? ValidateOpenXmlContainer(Stream stream)
    {
        if (!stream.CanSeek || stream.Length < 4)
        {
            return null;
        }

        var originalPosition = stream.Position;
        Span<byte> signature = stackalloc byte[4];
        stream.ReadExactly(signature);
        stream.Position = originalPosition;

        if (signature[0] != (byte)'P' || signature[1] != (byte)'K')
        {
            return null;
        }

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        long totalUncompressedBytes = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.Length > _options.MaxUncompressedOpenXmlBytes
                || totalUncompressedBytes > _options.MaxUncompressedOpenXmlBytes - entry.Length)
            {
                stream.Position = originalPosition;
                return InvalidWorkbook(
                    "The workbook expands beyond the supported resource limit and was not processed.");
            }

            totalUncompressedBytes += entry.Length;
        }

        stream.Position = originalPosition;
        return null;
    }

    private AttendanceWorkbookParseResult ParseFirstWorksheet(
        IExcelDataReader reader,
        CancellationToken cancellationToken)
    {
        var rows = new List<AttendanceImportRow>();
        var errors = new ImportErrorCollector(_options.MaxStoredErrors);
        var seenAttendance = new Dictionary<(int EmployeeCode, DateTime Date), int>();
        var physicalRowCount = 0;

        if (reader.FieldCount > AttendanceImportSchema.ColumnCount)
        {
            errors.Add(null, "Workbook",
                $"The first worksheet must contain exactly {AttendanceImportSchema.ColumnCount} columns; extra columns are not supported.");
            return new([], errors.Errors, errors.TotalCount);
        }

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            physicalRowCount++;

            if (physicalRowCount > _options.MaxWorksheetRows)
            {
                errors.Add(null, "Workbook",
                    $"The first worksheet exceeds the maximum of {_options.MaxWorksheetRows:N0} rows.");
                return new(rows, errors.Errors, errors.TotalCount);
            }

            if (IsBlankRow(reader))
            {
                continue;
            }

            if (physicalRowCount == 1 && LooksLikeHeader(reader))
            {
                errors.Add(1, "Workbook",
                    "Header rows are not supported. The first row must contain attendance data.");
                continue;
            }

            if (reader.FieldCount < AttendanceImportSchema.ColumnCount)
            {
                for (var missingColumn = reader.FieldCount;
                     missingColumn < AttendanceImportSchema.ColumnCount;
                     missingColumn++)
                {
                    var field = AttendanceImportSchema.FieldNames[missingColumn];
                    errors.Add(physicalRowCount, field, $"{field} is required.");
                }

                continue;
            }

            var rowErrorCount = errors.TotalCount;
            var employeeCode = ParseEmployeeCode(
                reader.GetValue(AttendanceImportSchema.EmployeeCodeColumn),
                physicalRowCount,
                errors);
            var date = ParseDate(
                reader.GetValue(AttendanceImportSchema.DateColumn),
                physicalRowCount,
                errors);
            var attendance = ParseTime(
                reader.GetValue(AttendanceImportSchema.AttendanceColumn),
                physicalRowCount,
                AttendanceImportSchema.FieldNames[AttendanceImportSchema.AttendanceColumn],
                errors);
            var departure = ParseTime(
                reader.GetValue(AttendanceImportSchema.DepartureColumn),
                physicalRowCount,
                AttendanceImportSchema.FieldNames[AttendanceImportSchema.DepartureColumn],
                errors);

            if (errors.TotalCount != rowErrorCount
                || employeeCode is null
                || date is null
                || attendance is null
                || departure is null)
            {
                continue;
            }

            if (departure.Value < attendance.Value)
            {
                errors.Add(physicalRowCount, "Departure",
                    "Departure cannot be earlier than attendance. Overnight shifts are not supported by the current attendance model.");
                continue;
            }

            var key = (employeeCode.Value, date.Value.Date);
            if (seenAttendance.TryGetValue(key, out var firstRow))
            {
                errors.Add(physicalRowCount, "Duplicate",
                    $"Duplicate attendance for employee {employeeCode.Value} on {date.Value:yyyy-MM-dd}; first provided on row {firstRow}.");
                continue;
            }

            seenAttendance.Add(key, physicalRowCount);
            rows.Add(new AttendanceImportRow(
                physicalRowCount,
                employeeCode.Value,
                date.Value.Date,
                attendance.Value,
                departure.Value));
        }

        if (physicalRowCount == 0 || (rows.Count == 0 && errors.TotalCount == 0))
        {
            errors.Add(null, "Workbook", "The first worksheet is empty or contains only blank rows.");
        }

        return new(rows, errors.Errors, errors.TotalCount);
    }

    private static bool IsBlankRow(IExcelDataReader reader)
    {
        for (var column = 0; column < reader.FieldCount; column++)
        {
            if (!IsBlank(reader.GetValue(column)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool LooksLikeHeader(IExcelDataReader reader)
    {
        if (reader.FieldCount < AttendanceImportSchema.ColumnCount)
        {
            return false;
        }

        for (var column = 0; column < AttendanceImportSchema.ColumnCount; column++)
        {
            var value = Convert.ToString(reader.GetValue(column), CultureInfo.InvariantCulture)?.Trim();
            if (!string.Equals(value, AttendanceImportSchema.FieldNames[column], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static int? ParseEmployeeCode(object? value, int rowNumber, ImportErrorCollector errors)
    {
        if (IsBlank(value))
        {
            errors.Add(rowNumber, "Employee Code", "Employee code is required.");
            return null;
        }

        if (TryConvertToDecimal(value, out var numeric)
            && numeric > 0
            && numeric <= int.MaxValue
            && decimal.Truncate(numeric) == numeric)
        {
            return (int)numeric;
        }

        errors.Add(rowNumber, "Employee Code",
            $"Value {FormatValue(value)} is not a valid positive whole-number employee code.");
        return null;
    }

    private static DateTime? ParseDate(object? value, int rowNumber, ImportErrorCollector errors)
    {
        if (IsBlank(value))
        {
            errors.Add(rowNumber, "Date", "Date is required.");
            return null;
        }

        if (value is DateTime dateTime)
        {
            return dateTime.Date;
        }

        if (TryConvertToDouble(value, out var serial)
            && TryFromOaDate(serial, out var excelDate))
        {
            return excelDate.Date;
        }

        if (value is string text
            && DateTime.TryParse(
                text.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsed))
        {
            return parsed.Date;
        }

        errors.Add(rowNumber, "Date", $"Value {FormatValue(value)} is not a valid date.");
        return null;
    }

    private static TimeSpan? ParseTime(
        object? value,
        int rowNumber,
        string field,
        ImportErrorCollector errors)
    {
        if (IsBlank(value))
        {
            errors.Add(rowNumber, field, $"{field} time is required.");
            return null;
        }

        TimeSpan? time = value switch
        {
            DateTime dateTime => dateTime.TimeOfDay,
            TimeSpan timeSpan => timeSpan,
            _ => null
        };

        if (time is null && TryConvertToDouble(value, out var numeric) && numeric >= 0 && numeric < 1)
        {
            time = TimeSpan.FromDays(numeric);
        }

        if (time is null && value is string text)
        {
            var trimmed = text.Trim();
            if (TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out var parsedTime))
            {
                time = parsedTime;
            }
            else if (DateTime.TryParse(
                         trimmed,
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.AllowWhiteSpaces,
                         out var parsedDateTime))
            {
                time = parsedDateTime.TimeOfDay;
            }
        }

        if (time is not null && time.Value >= TimeSpan.Zero && time.Value < TimeSpan.FromDays(1))
        {
            return time.Value;
        }

        errors.Add(rowNumber, field, $"Value {FormatValue(value)} is not a valid time of day.");
        return null;
    }

    private static bool TryConvertToDecimal(object? value, out decimal result)
    {
        if (value is string text)
        {
            return decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }

        try
        {
            result = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            result = default;
            return false;
        }
    }

    private static bool TryConvertToDouble(object? value, out double result)
    {
        if (value is string text)
        {
            return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        try
        {
            result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return double.IsFinite(result);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            result = default;
            return false;
        }
    }

    private static bool TryFromOaDate(double value, out DateTime date)
    {
        try
        {
            date = DateTime.FromOADate(value);
            return true;
        }
        catch (ArgumentException)
        {
            date = default;
            return false;
        }
    }

    private static bool IsBlank(object? value)
        => value is null or DBNull || value is string text && string.IsNullOrWhiteSpace(text);

    private static string FormatValue(object? value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        return $"\"{text.Replace("\"", "'", StringComparison.Ordinal)}\"";
    }

    private static AttendanceWorkbookParseResult InvalidWorkbook(
        string message = "The uploaded content is not a valid supported Excel workbook.")
        => new(
            [],
            [new AttendanceImportError(null, "File", message)],
            1);

    private sealed class ImportErrorCollector
    {
        private readonly int _maximumStoredErrors;
        private readonly List<AttendanceImportError> _errors = [];

        public ImportErrorCollector(int maximumStoredErrors)
        {
            _maximumStoredErrors = Math.Max(1, maximumStoredErrors);
        }

        public int TotalCount { get; private set; }
        public IReadOnlyList<AttendanceImportError> Errors => _errors;

        public void Add(int? rowNumber, string field, string message)
        {
            TotalCount++;
            if (_errors.Count < _maximumStoredErrors)
            {
                _errors.Add(new AttendanceImportError(rowNumber, field, message));
            }
        }
    }
}
