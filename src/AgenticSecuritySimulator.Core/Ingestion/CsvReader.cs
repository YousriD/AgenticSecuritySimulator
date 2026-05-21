namespace AgenticSecuritySimulator.Core.Ingestion;

internal static class CsvReader
{
    public static List<IReadOnlyList<string>> ReadAllRows(string csvContent)
    {
        var rows = new List<IReadOnlyList<string>>();
        using var reader = new StringReader(csvContent);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            rows.Add(ParseLine(line));
        }
        return rows;
    }

    private static List<string> ParseLine(string line)
    {
        var values = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.Trim());
                current = "";
                continue;
            }

            current += ch;
        }

        values.Add(current.Trim());
        return values;
    }
}
