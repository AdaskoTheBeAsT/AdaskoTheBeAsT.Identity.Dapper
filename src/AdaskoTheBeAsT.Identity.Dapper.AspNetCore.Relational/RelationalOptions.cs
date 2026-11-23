using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal sealed partial class RelationalOptions
{
    public string? SchemaName { get; init; }

    public string? IdentitySchemaName { get; init; }

    public string TablePrefix { get; init; } = "identityjwt";

    public string UserClaimsTableName { get; init; } = "aspnetuserclaims";

    public string RoleClaimsTableName { get; init; } = "aspnetroleclaims";

    public string UserRolesTableName { get; init; } = "aspnetuserroles";

    public bool IncludeRoleClaimsInTransformation { get; init; }

    public bool EnableCleanupHostedService { get; init; }

    public TimeSpan CleanupInterval { get; init; }

    public int CleanupBatchSize { get; init; }

    public string? ConnectionString { get; init; }

    public Func<IServiceProvider, DbConnection>? ConnectionFactory { get; init; }

    public Func<string, object>? IdentityUserIdConverter { get; init; }

    public IEnumerable<string> Validate(bool validateConnection = true)
    {
        if (validateConnection && string.IsNullOrWhiteSpace(ConnectionString) && ConnectionFactory == null)
        {
            yield return "Specify a connection string or connection factory.";
        }

        if (!IsIdentifier(TablePrefix, 44) ||
            !IsOptionalIdentifier(SchemaName) ||
            !IsOptionalIdentifier(IdentitySchemaName) ||
            !IsIdentifier(UserClaimsTableName, 63) ||
            !IsIdentifier(RoleClaimsTableName, 63) ||
            !IsIdentifier(UserRolesTableName, 63))
        {
            yield return "Invalid JWT store SQL identifier.";
        }

        if (CleanupInterval <= TimeSpan.Zero ||
            CleanupInterval.TotalMilliseconds > uint.MaxValue - 1 ||
            CleanupBatchSize <= 0)
        {
            yield return "Cleanup requires a positive interval and batch size.";
        }
    }

    public void EnsureValid(bool validateConnection)
    {
        var failures = Validate(validateConnection).ToArray();
        if (failures.Length > 0)
        {
            throw new OptionsValidationException(Microsoft.Extensions.Options.Options.DefaultName, typeof(RelationalOptions), failures);
        }
    }

    private static bool IsOptionalIdentifier(string? value) =>
        string.IsNullOrEmpty(value) || IsIdentifier(value, 63);

    private static bool IsIdentifier(string? value, int maxLength) =>
        value != null && value.Length <= maxLength && IdentifierPattern().IsMatch(value);

    [GeneratedRegex(@"\A[A-Za-z_][A-Za-z0-9_]*\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex IdentifierPattern();
}
