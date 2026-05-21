using System.IO.Compression;
using System.Text;
using EnterpriseAutomation.Data;
using EnterpriseAutomation.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAutomation.Services.Reports;

public sealed record ReportExportResult(string FileName, string ContentType, byte[] Content);

public sealed class ReportExportService
{
    private readonly AppDbContext _db;

    public ReportExportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReportExportResult> ExportAsync(
        string report,
        string format,
        DateTime? from,
        DateTime? to,
        int? statusId,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedReport = report.Trim().ToLowerInvariant();
        var normalizedFormat = format.Trim().ToLowerInvariant();

        return normalizedReport switch
        {
            "orders" => await ExportOrdersAsync(normalizedFormat, from, to, cancellationToken),
            "requests" => await ExportRequestsAsync(normalizedFormat, statusId, cancellationToken),
            "clients" => await ExportClientsAsync(normalizedFormat, take, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported report '{report}'.")
        };
    }

    private async Task<ReportExportResult> ExportOrdersAsync(
        string format,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        var fromDate = (from ?? DateTime.Today.AddMonths(-1)).Date;
        var toDate = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        var rows = await _db.Orders
            .AsNoTracking()
            .Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client)
            .Include(x => x.PaymentStatus)
            .Include(x => x.ExecutionStatus)
            .Where(x => x.DueDate >= fromDate && x.DueDate <= toDate)
            .OrderBy(x => x.DueDate)
            .Select(x => new OrdersByPeriodItem(
                x.Id,
                x.ServiceRequest!.Client!.Name,
                x.Services,
                x.Amount,
                x.DueDate,
                x.PaymentStatus!.Name,
                x.ExecutionStatus!.Name))
            .ToListAsync(cancellationToken);

        return BuildExport(
            "orders",
            format,
            ["Номер", "Клиент", "Услуги", "Сумма", "Срок", "Оплата", "Исполнение"],
            rows.Select(row => new object[]
            {
                row.OrderId,
                row.ClientName,
                row.Services,
                row.Amount,
                row.DueDate,
                row.PaymentStatus,
                row.ExecutionStatus
            }).ToList());
    }

    private async Task<ReportExportResult> ExportRequestsAsync(
        string format,
        int? statusId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.RequestStatus)
            .Where(x => !statusId.HasValue || x.RequestStatusId == statusId)
            .GroupBy(x => new { x.RequestStatusId, Status = x.RequestStatus!.Name })
            .Select(x => new RequestsByStatusItem(x.Key.Status, x.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Status)
            .ToListAsync(cancellationToken);

        return BuildExport(
            "requests",
            format,
            ["Статус", "Количество"],
            rows.Select(row => new object[]
            {
                row.Status,
                row.Count
            }).ToList());
    }

    private async Task<ReportExportResult> ExportClientsAsync(
        string format,
        int take,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 100);

        var rows = await _db.Clients
            .AsNoTracking()
            .Select(x => new TopClientItem(
                x.Name,
                x.Requests.Count(r => r.Order != null),
                x.Requests.Where(r => r.Order != null).Sum(r => (decimal?)r.Order!.Amount) ?? 0))
            .OrderByDescending(x => x.TotalAmount)
            .ThenByDescending(x => x.OrdersCount)
            .ThenBy(x => x.ClientName)
            .Take(take)
            .ToListAsync(cancellationToken);

        return BuildExport(
            "clients",
            format,
            ["Клиент", "Количество заказов", "Общая сумма"],
            rows.Select(row => new object[]
            {
                row.ClientName,
                row.OrdersCount,
                row.TotalAmount
            }).ToList());
    }

    private static ReportExportResult BuildExport(
        string reportName,
        string format,
        IReadOnlyList<string> headers,
        IReadOnlyList<object[]> rows)
    {
        return format switch
        {
            "csv" => BuildCsv(reportName, headers, rows),
            "xlsx" => BuildXlsx(reportName, headers, rows),
            "pdf" => BuildPdf(reportName, headers, rows),
            _ => throw new InvalidOperationException($"Unsupported export format '{format}'.")
        };
    }

    private static ReportExportResult BuildCsv(
        string reportName,
        IReadOnlyList<string> headers,
        IReadOnlyList<object[]> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(";", headers.Select(EscapeCsvCell)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(";", row.Select(value => EscapeCsvCell(FormatCell(value)))));
        }

        var bytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(builder.ToString()))
            .ToArray();

        return new ReportExportResult(
            $"{reportName}-report-{DateTime.Today:yyyyMMdd}.csv",
            "text/csv; charset=utf-8",
            bytes);
    }

    private static ReportExportResult BuildXlsx(
        string reportName,
        IReadOnlyList<string> headers,
        IReadOnlyList<object[]> rows)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
            AddEntry(archive, "_rels/.rels", BuildRootRelationshipsXml());
            AddEntry(archive, "docProps/app.xml", BuildAppXml(reportName));
            AddEntry(archive, "docProps/core.xml", BuildCoreXml(reportName));
            AddEntry(archive, "xl/workbook.xml", BuildWorkbookXml(reportName));
            AddEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationshipsXml());
            AddEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(headers, rows));
        }

        return new ReportExportResult(
            $"{reportName}-report-{DateTime.Today:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            stream.ToArray());
    }

    private static ReportExportResult BuildPdf(
        string reportName,
        IReadOnlyList<string> headers,
        IReadOnlyList<object[]> rows)
    {
        var lines = new List<string>
        {
            Translit($"{reportName.ToUpperInvariant()} REPORT"),
            Translit($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm}")
        };

        lines.Add(string.Empty);
        lines.Add(Translit(string.Join(" | ", headers)));

        foreach (var row in rows.Take(36))
        {
            lines.Add(Translit(string.Join(" | ", row.Select(FormatCell))));
        }

        if (rows.Count > 36)
        {
            lines.Add(Translit($"... and {rows.Count - 36} more rows"));
        }

        var pdfBytes = MinimalPdfWriter.Write(lines);
        return new ReportExportResult(
            $"{reportName}-report-{DateTime.Today:yyyyMMdd}.pdf",
            "application/pdf",
            pdfBytes);
    }

    private static string FormatCell(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("dd.MM.yyyy"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("dd.MM.yyyy"),
            decimal money => money.ToString("N2"),
            double money => money.ToString("N2"),
            float money => money.ToString("N2"),
            _ => Convert.ToString(value) ?? string.Empty
        };
    }

    private static string EscapeCsvCell(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string BuildContentTypesXml() => """
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
  <Default Extension="xml" ContentType="application/xml" />
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml" />
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml" />
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml" />
</Types>
""";

    private static string BuildRootRelationshipsXml() => """
<?xml version="1.0" encoding="utf-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml" />
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml" />
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml" />
</Relationships>
""";

    private static string BuildWorkbookXml(string sheetName) => $"""
<?xml version="1.0" encoding="utf-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="{EscapeXml(sheetName)}" sheetId="1" r:id="rId1" />
  </sheets>
</workbook>
""";

    private static string BuildWorkbookRelationshipsXml() => """
<?xml version="1.0" encoding="utf-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml" />
</Relationships>
""";

    private static string BuildAppXml(string reportName) => $"""
<?xml version="1.0" encoding="utf-8"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"
            xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Enterprise Automation</Application>
  <TitlesOfParts>
    <vt:vector size="1" baseType="lpstr">
      <vt:lpstr>{EscapeXml(reportName)}</vt:lpstr>
    </vt:vector>
  </TitlesOfParts>
</Properties>
""";

    private static string BuildCoreXml(string reportName) => $"""
<?xml version="1.0" encoding="utf-8"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties"
                   xmlns:dc="http://purl.org/dc/elements/1.1/"
                   xmlns:dcterms="http://purl.org/dc/terms/"
                   xmlns:dcmitype="http://purl.org/dc/dcmitype/"
                   xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>{EscapeXml(reportName)}</dc:title>
  <dc:creator>Enterprise Automation</dc:creator>
  <cp:lastModifiedBy>Enterprise Automation</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">{DateTime.UtcNow:O}</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">{DateTime.UtcNow:O}</dcterms:modified>
</cp:coreProperties>
""";

    private static string BuildWorksheetXml(IReadOnlyList<string> headers, IReadOnlyList<object[]> rows)
    {
        var allRows = new List<object[]>
        {
            headers.Cast<object>().ToArray()
        };
        allRows.AddRange(rows);

        var rowXml = new StringBuilder();
        for (var rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
        {
            rowXml.Append($"<row r=\"{rowIndex + 1}\">");
            var row = allRows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
            {
                var cellRef = $"{ExcelColumnName(columnIndex + 1)}{rowIndex + 1}";
                rowXml.Append($"<c r=\"{cellRef}\" t=\"inlineStr\"><is><t>{EscapeXml(FormatCell(row[columnIndex]))}</t></is></c>");
            }
            rowXml.Append("</row>");
        }

        return $"""
<?xml version="1.0" encoding="utf-8"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>{rowXml}</sheetData>
</worksheet>
""";
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private static string ExcelColumnName(int columnNumber)
    {
        var dividend = columnNumber;
        var columnName = string.Empty;

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    private static string Translit(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var map = new Dictionary<char, string>
        {
            ['А'] = "A", ['Б'] = "B", ['В'] = "V", ['Г'] = "G", ['Д'] = "D", ['Е'] = "E", ['Ё'] = "E",
            ['Ж'] = "Zh", ['З'] = "Z", ['И'] = "I", ['Й'] = "Y", ['К'] = "K", ['Л'] = "L", ['М'] = "M",
            ['Н'] = "N", ['О'] = "O", ['П'] = "P", ['Р'] = "R", ['С'] = "S", ['Т'] = "T", ['У'] = "U",
            ['Ф'] = "F", ['Х'] = "Kh", ['Ц'] = "Ts", ['Ч'] = "Ch", ['Ш'] = "Sh", ['Щ'] = "Sch", ['Ъ'] = string.Empty,
            ['Ы'] = "Y", ['Ь'] = string.Empty, ['Э'] = "E", ['Ю'] = "Yu", ['Я'] = "Ya",
            ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "e",
            ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m",
            ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
            ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sch", ['ъ'] = string.Empty,
            ['ы'] = "y", ['ь'] = string.Empty, ['э'] = "e", ['ю'] = "yu", ['я'] = "ya"
        };

        var builder = new StringBuilder(value.Length * 2);
        foreach (var character in value)
        {
            if (character is >= ' ' and <= '~')
            {
                builder.Append(character);
                continue;
            }

            if (map.TryGetValue(character, out var replacement))
            {
                builder.Append(replacement);
            }
        }

        return builder.ToString();
    }

    private static class MinimalPdfWriter
    {
        public static byte[] Write(IReadOnlyList<string> lines)
        {
            var normalizedLines = lines.Select(Translit).ToList();
            var content = BuildContent(normalizedLines);

            var objects = new List<string>
            {
                "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
                "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj",
                "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj",
                "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj",
                $"5 0 obj << /Length {Encoding.ASCII.GetByteCount(content)} >> stream\n{content}\nendstream endobj"
            };

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
            writer.Write("PDF-1.4\n");

            var offsets = new List<int>();
            foreach (var obj in objects)
            {
                offsets.Add((int)stream.Position);
                writer.Write(obj);
                writer.Write("\n");
                writer.Flush();
            }

            var xrefPosition = (int)stream.Position;
            writer.Write("xref\n0 6\n");
            writer.Write("0000000000 65535 f \n");
            foreach (var offset in offsets)
            {
                writer.Write($"{offset:0000000000} 00000 n \n");
            }

            writer.Write("trailer << /Size 6 /Root 1 0 R >>\n");
            writer.Write("startxref\n");
            writer.Write(xrefPosition);
            writer.Write("\n%%EOF");
            writer.Flush();

            return stream.ToArray();
        }

        private static string BuildContent(IReadOnlyList<string> lines)
        {
            var builder = new StringBuilder();
            builder.AppendLine("BT");
            builder.AppendLine("/F1 10 Tf");
            builder.AppendLine("50 800 Td");

            var firstLine = true;
            foreach (var line in lines)
            {
                var text = EscapePdfText(line);
                if (firstLine)
                {
                    builder.AppendLine($"({text}) Tj");
                    firstLine = false;
                }
                else
                {
                    builder.AppendLine("0 -14 Td");
                    builder.AppendLine($"({text}) Tj");
                }
            }

            builder.AppendLine("ET");
            return builder.ToString();
        }

        private static string EscapePdfText(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)")
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);
        }
    }
}
