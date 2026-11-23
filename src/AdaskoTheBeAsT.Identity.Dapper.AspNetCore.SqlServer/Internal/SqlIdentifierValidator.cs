using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Internal;

internal static partial class SqlIdentifierValidator
{
    public static bool IsValid(string? identifier) =>
        !string.IsNullOrWhiteSpace(identifier) && IdentifierRegex().IsMatch(identifier);

    public static string QuoteIdentifier(
        string identifier,
        string parameterDescription)
    {
        if (!IsValid(identifier))
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Invalid SQL identifier configured for {parameterDescription}."));
        }

        return string.Create(CultureInfo.InvariantCulture, $"[{identifier}]");
    }

    public static string QuoteQualifiedTable(
        string schemaName,
        string tableName)
    {
        var quotedSchema = QuoteIdentifier(schemaName, nameof(schemaName));
        var quotedTable = QuoteIdentifier(tableName, nameof(tableName));
        return string.Create(CultureInfo.InvariantCulture, $"{quotedSchema}.{quotedTable}");
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IdentifierRegex();
}
