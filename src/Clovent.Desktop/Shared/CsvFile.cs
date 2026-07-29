using System.Text;

namespace Clovent.Desktop.Shared;

/// <summary>
/// Minimal RFC 4180-style CSV reader/writer backing every Milestone 14
/// ("Product Catalog &amp; Inventory Foundation") screen's Export/Import CSV
/// buttons (<c>MasterDataListView{TDto}.OnExportCsv</c>/<c>OnImportCsv</c>).
/// Kept as pure string-in/string-out logic - no <c>System.IO</c> beyond the
/// two file-path entry points - so parsing/escaping can be unit tested
/// without a Windows Forms message loop, the same reasoning already applied
/// to <c>Clovent.Desktop.MasterData.MasterDataFilter</c>.
/// </summary>
public static class CsvFile
{
    /// <summary>Writes a header row followed by every data row to <paramref name="path"/>, overwriting any existing file.</summary>
    public static void Write(string path, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var lines = new List<string> { FormatRow(headers) };
        lines.AddRange(rows.Select(FormatRow));

        File.WriteAllLines(path, lines, Encoding.UTF8);
    }

    /// <summary>Reads every data row from <paramref name="path"/>, skipping the header row. Blank lines are skipped.</summary>
    public static IReadOnlyList<IReadOnlyList<string>> ReadDataRows(string path) =>
        ParseDataRows(File.ReadAllLines(path));

    /// <summary>Parses every data row from already-read <paramref name="lines"/> (including the header, which is skipped) - the testable core <see cref="ReadDataRows"/> wraps.</summary>
    public static IReadOnlyList<IReadOnlyList<string>> ParseDataRows(IReadOnlyList<string> lines) =>
        [.. lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).Select(ParseRow)];

    /// <summary>Formats one row, quoting any field containing a comma, quote, or newline.</summary>
    public static string FormatRow(IReadOnlyList<string> fields) => string.Join(',', fields.Select(Escape));

    /// <summary>Parses one CSV row into its fields, honoring double-quote escaping.</summary>
    public static IReadOnlyList<string> ParseRow(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static string Escape(string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
