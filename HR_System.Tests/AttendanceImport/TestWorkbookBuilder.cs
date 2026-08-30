using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace HR_System.Tests.AttendanceImport;

internal static class TestWorkbookBuilder
{
    public static MemoryStream CreateXlsx(
        IReadOnlyList<object?[]> rows,
        int? declaredColumnCount = null,
        bool includeWorksheet = true)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypes(includeWorksheet));
            WriteEntry(archive, "_rels/.rels", RootRelationships);
            WriteEntry(archive, "xl/workbook.xml", Workbook(includeWorksheet));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships(includeWorksheet));

            if (includeWorksheet)
            {
                WriteEntry(archive, "xl/worksheets/sheet1.xml", Worksheet(rows, declaredColumnCount));
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static string Worksheet(IReadOnlyList<object?[]> rows, int? declaredColumnCount)
    {
        var maximumColumns = declaredColumnCount
            ?? (rows.Count == 0 ? 1 : rows.Max(row => Math.Max(1, row.Length)));
        var dimension = rows.Count == 0
            ? "A1"
            : $"A1:{ColumnName(maximumColumns)}{rows.Count}";
        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>")
            .Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">")
            .Append("<dimension ref=\"").Append(dimension).Append("\"/><sheetData>");

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            xml.Append("<row r=\"").Append(rowIndex + 1).Append("\">");
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
            {
                var value = row[columnIndex];
                if (value is null)
                {
                    continue;
                }

                var reference = $"{ColumnName(columnIndex + 1)}{rowIndex + 1}";
                if (value is string text)
                {
                    xml.Append("<c r=\"").Append(reference).Append("\" t=\"inlineStr\"><is><t>")
                        .Append(SecurityElement.Escape(text))
                        .Append("</t></is></c>");
                }
                else
                {
                    xml.Append("<c r=\"").Append(reference).Append("\"><v>")
                        .Append(Convert.ToString(value, CultureInfo.InvariantCulture))
                        .Append("</v></c>");
                }
            }

            xml.Append("</row>");
        }

        return xml.Append("</sheetData></worksheet>").ToString();
    }

    private static string ColumnName(int oneBasedColumn)
    {
        var name = string.Empty;
        var value = oneBasedColumn;
        while (value > 0)
        {
            value--;
            name = (char)('A' + value % 26) + name;
            value /= 26;
        }

        return name;
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string ContentTypes(bool includeWorksheet)
        => "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
           + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
           + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"
           + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
           + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>"
           + (includeWorksheet
               ? "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"
               : string.Empty)
           + "</Types>";

    private const string RootRelationships =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
        + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
        + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>"
        + "</Relationships>";

    private static string Workbook(bool includeWorksheet)
        => "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
           + "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">"
           + "<sheets>"
           + (includeWorksheet ? "<sheet name=\"Attendance\" sheetId=\"1\" r:id=\"rId1\"/>" : string.Empty)
           + "</sheets></workbook>";

    private static string WorkbookRelationships(bool includeWorksheet)
        => "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
           + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
           + (includeWorksheet
               ? "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>"
               : string.Empty)
           + "</Relationships>";
}
