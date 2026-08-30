using HR_System.AttendanceImport;
using HR_System.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HR_System.Tests.AttendanceImport;

public sealed class AttendanceImportServiceTests
{
    [Fact]
    public async Task Valid_workbook_imports_all_rows_with_one_save_pipeline()
    {
        await using var database = CreateDatabase();
        database.Employees.Add(CreateEmployee(15));
        await database.SaveChangesAsync();
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            [15d, new DateTime(2026, 8, 30).ToOADate(), 0.375d, 0.7083333333d],
            [15d, new DateTime(2026, 8, 31).ToOADate(), "09:30", "17:15"]
        ]);

        var result = await CreateService(database).ImportAsync(workbook, "attendance.xlsx");

        Assert.True(result.Success);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(2, await database.Att_dep.CountAsync());
    }

    [Fact]
    public async Task Empty_file_is_rejected_without_database_changes()
    {
        await using var database = CreateDatabase();
        await using var stream = new MemoryStream();

        var result = await CreateService(database).ImportAsync(stream, "attendance.xlsx");

        Assert.False(result.Success);
        Assert.Equal(0, await database.Att_dep.CountAsync());
    }

    [Fact]
    public async Task Malformed_non_excel_content_is_a_controlled_failure()
    {
        await using var database = CreateDatabase();
        await using var stream = new MemoryStream("this is not excel"u8.ToArray());

        var result = await CreateService(database).ImportAsync(stream, "attendance.xlsx");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Field == "File");
        Assert.Equal(0, await database.Att_dep.CountAsync());
    }

    [Fact]
    public async Task Unsupported_extension_is_rejected_before_parsing()
    {
        await using var database = CreateDatabase();
        await using var stream = new MemoryStream("content"u8.ToArray());

        var result = await CreateService(database).ImportAsync(stream, "attendance.csv");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Message.Contains(".xls", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Unknown_employee_code_is_rejected_before_persistence()
    {
        await using var database = CreateDatabase();
        using var workbook = SingleValidRow(employeeCode: 999, date: new DateTime(2026, 8, 30));

        var result = await CreateService(database).ImportAsync(workbook, "attendance.xlsx");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Message.Contains("999 does not exist", StringComparison.Ordinal));
        Assert.Equal(0, await database.Att_dep.CountAsync());
    }

    [Fact]
    public async Task Existing_employee_date_is_rejected_without_overwrite()
    {
        await using var database = CreateDatabase();
        database.Employees.Add(CreateEmployee(15));
        database.Att_dep.Add(new AttDep
        {
            EmpId = 15,
            Date = new DateTime(2026, 8, 30),
            Attendance = new TimeSpan(8, 0, 0),
            Departure = new TimeSpan(16, 0, 0)
        });
        await database.SaveChangesAsync();
        using var workbook = SingleValidRow(15, new DateTime(2026, 8, 30));

        var result = await CreateService(database).ImportAsync(workbook, "attendance.xlsx");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Field == "Duplicate");
        var existing = Assert.Single(await database.Att_dep.AsNoTracking().ToListAsync());
        Assert.Equal(new TimeSpan(8, 0, 0), existing.Attendance);
    }

    [Fact]
    public async Task Within_file_duplicate_rejection_persists_zero_rows()
    {
        await using var database = CreateDatabase();
        database.Employees.Add(CreateEmployee(15));
        await database.SaveChangesAsync();
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            [15d, 46_000d, 0.375d, 0.7083333333d],
            [15d, 46_000d, 0.4d, 0.75d]
        ]);

        var result = await CreateService(database).ImportAsync(workbook, "attendance.xlsx");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Field == "Duplicate");
        Assert.Equal(0, await database.Att_dep.CountAsync());
    }

    [Fact]
    public async Task Mixed_valid_and_invalid_rows_leave_database_unchanged()
    {
        await using var database = CreateDatabase();
        database.Employees.Add(CreateEmployee(15));
        await database.SaveChangesAsync();
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            [15d, 46_000d, 0.375d, 0.7083333333d],
            [15d, 46_001d, 0.375d, 0.7083333333d],
            [15d, "invalid-date", 0.375d, 0.7083333333d]
        ]);

        var result = await CreateService(database).ImportAsync(workbook, "attendance.xlsx");

        Assert.False(result.Success);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, await database.Att_dep.CountAsync());
    }

    [Fact]
    public async Task Stream_is_bounded_even_when_length_metadata_is_unavailable()
    {
        await using var database = CreateDatabase();
        await using var stream = new GeneratedStream(129);
        var options = new AttendanceImportOptions { MaxUploadBytes = 128 };

        var result = await CreateService(database, options).ImportAsync(stream, "attendance.xlsx");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Message.Contains("upload limit", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(10 * 1024 * 1024, AttendanceImportDefaults.MaxUploadBytes);
    }

    private static HrSysContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<HrSysContext>()
            .UseInMemoryDatabase($"attendance-import-{Guid.NewGuid():N}")
            .Options;
        return new HrSysContext(options);
    }

    private static AttendanceImportService CreateService(
        HrSysContext database,
        AttendanceImportOptions? options = null)
    {
        options ??= new AttendanceImportOptions();
        var wrappedOptions = Options.Create(options);
        return new AttendanceImportService(
            database,
            new AttendanceWorkbookParser(wrappedOptions),
            wrappedOptions,
            NullLogger<AttendanceImportService>.Instance);
    }

    private static MemoryStream SingleValidRow(int employeeCode, DateTime date)
        => TestWorkbookBuilder.CreateXlsx(
        [
            [(double)employeeCode, date.ToOADate(), 0.375d, 0.7083333333d]
        ]);

    private static Employee CreateEmployee(int employeeCode)
        => new()
        {
            EmpId = employeeCode,
            EmpName = $"Employee {employeeCode}",
            Address = "Test Address",
            Phone = "01000000000",
            Gender = "Male",
            Nationality = "Egyptian",
            Birthdate = new DateTime(1990, 1, 1),
            NationalId = $"{employeeCode:D14}",
            Hiredate = new DateTime(2020, 1, 1),
            FixedSalary = 10_000,
            AttTime = new TimeSpan(9, 0, 0),
            DepartureTime = new TimeSpan(17, 0, 0)
        };

    private sealed class GeneratedStream : Stream
    {
        private readonly long _length;
        private long _position;

        public GeneratedStream(long length)
        {
            _length = length;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = _length - _position;
            if (remaining <= 0)
            {
                return 0;
            }

            var bytes = (int)Math.Min(count, remaining);
            Array.Clear(buffer, offset, bytes);
            _position += bytes;
            return bytes;
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = _length - _position;
            if (remaining <= 0)
            {
                return ValueTask.FromResult(0);
            }

            var bytes = (int)Math.Min(buffer.Length, remaining);
            buffer.Span[..bytes].Clear();
            _position += bytes;
            return ValueTask.FromResult(bytes);
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
