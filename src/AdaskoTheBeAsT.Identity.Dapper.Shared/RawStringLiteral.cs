using System;
using System.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

internal static class RawStringLiteral
{
    public static string Format(string value, string indentation = "            ")
    {
        // A delimiter must be longer than every quote sequence in the SQL, including mapped names.
        var quoteCount = 3;
        var consecutiveQuotes = 0;
        foreach (var character in value)
        {
            consecutiveQuotes = character == '"' ? consecutiveQuotes + 1 : 0;
            quoteCount = Math.Max(quoteCount, consecutiveQuotes + 1);
        }

        var delimiter = new string('"', quoteCount);
        var result = new StringBuilder();
        result.Append(delimiter).Append("\r\n").Append(indentation);
        for (var i = 0; i < value.Length; i++)
        {
            result.Append(value[i]);
            if (value[i] is '\n' or '\u0085' or '\u2028' or '\u2029' ||
                (value[i] == '\r' && (i + 1 == value.Length || value[i + 1] != '\n')))
            {
                result.Append(indentation);
            }
        }

        // Indent every content line equally so the compiler preserves the original SQL whitespace.
        return result.Append("\r\n").Append(indentation).Append(delimiter).ToString();
    }
}
