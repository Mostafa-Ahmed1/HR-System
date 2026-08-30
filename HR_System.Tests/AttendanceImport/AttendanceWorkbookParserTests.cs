using HR_System.AttendanceImport;
using Microsoft.Extensions.Options;
using Xunit;

namespace HR_System.Tests.AttendanceImport;

public sealed class AttendanceWorkbookParserTests
{
    [Fact]
    public void Valid_headerless_workbook_is_parsed_with_numeric_and_string_values()
    {
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            [15d, new DateTime(2026, 8, 30).ToOADate(), 9d / 24, 17.5d / 24],
            ["16", "2026-08-31", "09:15", "5:30 PM"]
        ]);

        var result = CreateParser().Parse(workbook);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(new DateTime(2026, 8, 30), result.Rows[0].Date);
        Assert.Equal(new TimeSpan(9, 0, 0), result.Rows[0].Attendance);
        Assert.Equal(new TimeSpan(17, 30, 0), result.Rows[0].Departure);
    }

    [Fact]
    public void Empty_worksheet_is_rejected()
    {
        using var workbook = TestWorkbookBuilder.CreateXlsx([]);

        var result = CreateParser().Parse(workbook);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message.Contains("empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Workbook_without_a_worksheet_is_rejected_safely()
    {
        using var workbook = TestWorkbookBuilder.CreateXlsx([], includeWorksheet: false);

        var result = CreateParser().Parse(workbook);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Header_only_worksheet_is_rejected_and_not_treated_as_data()
    {
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            ["Employee Code", "Date", "Attendance", "Departure"]
        ]);

        var result = CreateParser().Parse(workbook);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message.Contains("Header", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0, "Employee Code")]
    [InlineData(1, "Date")]
    [InlineData(2, "Attendance")]
    [InlineData(3, "Departure")]
    public void Missing_required_value_is_rejected(int missingColumn, string expectedField)
    {
        object?[] row = [15d, new DateTime(2026, 8, 30).ToOADate(), 0.375d, 0.7083333333d];
        row[missingColumn] = null;
        using var workbook = TestWorkbookBuilder.CreateXlsx([row], declaredColumnCount: 4);

        var result = CreateParser().Parse(workbook);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == expectedField);
    }

    [Theory]
    [InlineData(0, "15.5", "Employee Code")]
    [InlineData(1, "not-a-date", "Date")]
    [InlineData(2, "abc", "Attendance")]
    [InlineData(3, "25:00", "Departure")]
    public void Malformed_cell_value_is_rejected(int column, string malformedValue, string expectedField)
    {
        object?[] row = [15d, new DateTime(2026, 8, 30).ToOADate(), 0.375d, 0.7083333333d];
        row[column] = malformedValue;
        using var workbook = TestWorkbookBuilder.CreateXlsx([row]);

        var result = CreateParser().Parse(workbook);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.RowNumber == 1 && error.Field == expectedField);
    }

    [Fact]
    public void Blank_rows_are_skipped_but_preserve_source_row_numbers()
    {
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            [null, null, null, null],
            [15d, new DateTime(2026, 8, 30).ToOADate(), 0.375d, 0.7083333333d],
            ["  ", null, null, null]
        ], declaredColumnCount: 4);

        var result = CreateParser().Parse(workbook);

        Assert.True(result.IsValid);
        var row = Assert.Single(result.Rows);
        Assert.Equal(2, row.RowNumber);
    }

    [Fact]
    public void Extra_columns_are_rejected()
    {
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            [15d, 46_000d, 0.375d, 0.7083333333d, "unexpected"]
        ]);

        var result = CreateParser().Parse(workbook);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message.Contains("extra columns", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Maximum_physical_row_limit_is_enforced_including_blank_rows()
    {
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            [15d, 46_000d, 0.375d, 0.7083333333d],
            [null, null, null, null],
            [15d, 46_001d, 0.375d, 0.7083333333d]
        ], declaredColumnCount: 4);

        var result = CreateParser(maxRows: 2).Parse(workbook);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message.Contains("maximum", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(50_000, AttendanceImportDefaults.MaxWorksheetRows);
    }

    [Fact]
    public void OpenXml_uncompressed_size_limit_is_enforced_before_cell_parsing()
    {
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            [15d, 46_000d, 0.375d, 0.7083333333d, new string('x', 2_000)]
        ]);
        var parser = new AttendanceWorkbookParser(Options.Create(new AttendanceImportOptions
        {
            MaxUncompressedOpenXmlBytes = 1_000
        }));

        var result = parser.Parse(workbook);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message.Contains("resource limit", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(100 * 1024 * 1024, AttendanceImportDefaults.MaxUncompressedOpenXmlBytes);
    }

    [Fact]
    public void Duplicate_employee_and_date_inside_workbook_is_rejected()
    {
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            [15d, 46_000d, 0.375d, 0.7083333333d],
            [15d, 46_000d, 0.4d, 0.75d]
        ]);

        var result = CreateParser().Parse(workbook);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.RowNumber == 2 && error.Field == "Duplicate");
    }

    [Fact]
    public void Overnight_shift_is_rejected_explicitly()
    {
        using var workbook = TestWorkbookBuilder.CreateXlsx(
        [
            [15d, 46_000d, 22d / 24, 6d / 24]
        ]);

        var result = CreateParser().Parse(workbook);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message.Contains("Overnight", StringComparison.OrdinalIgnoreCase));
    }

    private static AttendanceWorkbookParser CreateParser(int maxRows = AttendanceImportDefaults.MaxWorksheetRows)
        => new(Options.Create(new AttendanceImportOptions
        {
            MaxWorksheetRows = maxRows
        }));
}
