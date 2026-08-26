using System.Globalization;

namespace Alkanzi.DemoDataImporter;

/// <summary>
/// Parses an Oracle SQL Developer dump ("Insert into SCHEMA.TABLE (cols) values (vals);")
/// into column→value dictionaries. Values are materialized as CLR objects: DateTime for
/// to_timestamp(...), null for NULL, string for quoted literals, and string for raw numbers
/// (converted to the target type at load time).
/// </summary>
public static class OracleInsertParser
{
    public static IEnumerable<Dictionary<string, object?>> Parse(string sqlText)
    {
        foreach (var stmt in SplitStatements(sqlText))
        {
            var s = stmt.TrimStart();
            if (!s.StartsWith("Insert into", StringComparison.OrdinalIgnoreCase))
                continue;

            var row = ParseInsert(s);
            if (row != null) yield return row;
        }
    }

    // --- split a script into statements on ';' that are outside string literals ---
    private static IEnumerable<string> SplitStatements(string text)
    {
        var start = 0;
        var inStr = false;
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\'')
            {
                if (inStr && i + 1 < text.Length && text[i + 1] == '\'') { i++; continue; } // escaped ''
                inStr = !inStr;
            }
            else if (c == ';' && !inStr)
            {
                yield return text.Substring(start, i - start);
                start = i + 1;
            }
        }
        if (start < text.Length) yield return text.Substring(start);
    }

    private static Dictionary<string, object?>? ParseInsert(string stmt)
    {
        var open = stmt.IndexOf('(');
        if (open < 0) return null;
        var close = MatchParen(stmt, open);
        if (close < 0) return null;

        var cols = SplitTopLevel(stmt.Substring(open + 1, close - open - 1))
            .Select(CleanColumn).ToList();

        var vIdx = stmt.IndexOf("values", close, StringComparison.OrdinalIgnoreCase);
        if (vIdx < 0) return null;
        var vOpen = stmt.IndexOf('(', vIdx);
        if (vOpen < 0) return null;
        var vClose = MatchParen(stmt, vOpen);
        if (vClose < 0) return null;

        var vals = SplitTopLevel(stmt.Substring(vOpen + 1, vClose - vOpen - 1)).ToList();
        if (vals.Count != cols.Count) return null; // malformed row — skip

        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < cols.Count; i++)
            map[cols[i]] = ConvertValueToken(vals[i].Trim());
        return map;
    }

    private static int MatchParen(string s, int openIdx)
    {
        var depth = 0; var inStr = false;
        for (int i = openIdx; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\'')
            {
                if (inStr && i + 1 < s.Length && s[i + 1] == '\'') { i++; continue; }
                inStr = !inStr;
            }
            else if (!inStr && c == '(') depth++;
            else if (!inStr && c == ')') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    // split on top-level commas, ignoring commas inside strings or nested parens (to_timestamp(...))
    private static IEnumerable<string> SplitTopLevel(string s)
    {
        var start = 0; var depth = 0; var inStr = false;
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\'')
            {
                if (inStr && i + 1 < s.Length && s[i + 1] == '\'') { i++; continue; }
                inStr = !inStr;
            }
            else if (!inStr && c == '(') depth++;
            else if (!inStr && c == ')') depth--;
            else if (!inStr && c == ',' && depth == 0)
            {
                yield return s.Substring(start, i - start);
                start = i + 1;
            }
        }
        yield return s.Substring(start);
    }

    private static string CleanColumn(string c)
    {
        c = c.Trim();
        if (c.Length >= 2 && c[0] == '"' && c[^1] == '"') c = c.Substring(1, c.Length - 2);
        return c;
    }

    private static object? ConvertValueToken(string v)
    {
        if (v.Length == 0 || v.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        if (v.StartsWith("to_timestamp(", StringComparison.OrdinalIgnoreCase) ||
            v.StartsWith("to_date(", StringComparison.OrdinalIgnoreCase))
        {
            var q1 = v.IndexOf('\'');
            var q2 = q1 >= 0 ? v.IndexOf('\'', q1 + 1) : -1;
            if (q1 >= 0 && q2 > q1)
                return ParseOracleTimestamp(v.Substring(q1 + 1, q2 - q1 - 1));
            return null;
        }

        if (v[0] == '\'')
        {
            var inner = v.Length >= 2 && v[^1] == '\'' ? v.Substring(1, v.Length - 2) : v.Substring(1);
            return inner.Replace("''", "'"); // string literal
        }

        return v; // raw number (as string) — converted to the target type at load
    }

    private static readonly Dictionary<string, int> Months = new(StringComparer.OrdinalIgnoreCase)
    {
        ["JAN"] = 1, ["FEB"] = 2, ["MAR"] = 3, ["APR"] = 4, ["MAY"] = 5, ["JUN"] = 6,
        ["JUL"] = 7, ["AUG"] = 8, ["SEP"] = 9, ["OCT"] = 10, ["NOV"] = 11, ["DEC"] = 12,
    };

    /// <summary>Parses e.g. "03-MAR-22 12.32.44.433878400 PM" (Oracle DD-MON-RR HH.MI.SSXFF AM).</summary>
    public static DateTime? ParseOracleTimestamp(string s)
    {
        try
        {
            var parts = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            var d = parts[0].Split('-');
            if (d.Length != 3 || !Months.TryGetValue(d[1], out var month)) return null;
            var day = int.Parse(d[0], CultureInfo.InvariantCulture);
            var yy = int.Parse(d[2], CultureInfo.InvariantCulture);
            var year = d[2].Length <= 2 ? (yy < 50 ? 2000 + yy : 1900 + yy) : yy; // Oracle RR

            int hour = 0, min = 0, sec = 0; long fracTicks = 0;
            var t = parts[1].Split('.');
            if (t.Length >= 1) hour = int.Parse(t[0], CultureInfo.InvariantCulture);
            if (t.Length >= 2) min = int.Parse(t[1], CultureInfo.InvariantCulture);
            if (t.Length >= 3) sec = int.Parse(t[2], CultureInfo.InvariantCulture);
            if (t.Length >= 4)
            {
                var frac = t[3].PadRight(7, '0').Substring(0, 7); // 100ns ticks
                fracTicks = long.Parse(frac, CultureInfo.InvariantCulture);
            }

            if (parts.Length >= 3)
            {
                var ampm = parts[2].ToUpperInvariant();
                if (ampm == "PM" && hour < 12) hour += 12;
                else if (ampm == "AM" && hour == 12) hour = 0;
            }

            return new DateTime(year, month, day, hour, min, sec, DateTimeKind.Unspecified).AddTicks(fracTicks);
        }
        catch { return null; }
    }
}
