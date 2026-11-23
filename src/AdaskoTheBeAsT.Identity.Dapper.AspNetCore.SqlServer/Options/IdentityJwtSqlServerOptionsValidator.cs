using System;
using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Internal;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;

public sealed class IdentityJwtSqlServerOptionsValidator : IValidateOptions<IdentityJwtSqlServerOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        IdentityJwtSqlServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString) && options.ConnectionFactory == null)
        {
            failures.Add("Either ConnectionString or ConnectionFactory must be configured for SQL Server JWT stores.");
        }

        ValidateIdentifiers(options, failures);

        if (options.CleanupInterval <= TimeSpan.Zero)
        {
            failures.Add("CleanupInterval must be greater than zero.");
        }

        if (options.CleanupBatchSize <= 0)
        {
            failures.Add("CleanupBatchSize must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateIdentifiers(
        IdentityJwtSqlServerOptions options,
        List<string> failures)
    {
        CheckIdentifier(options.SchemaName, nameof(options.SchemaName), failures);
        if (options.IdentitySchemaName != null)
        {
            CheckIdentifier(options.IdentitySchemaName, nameof(options.IdentitySchemaName), failures);
        }

        CheckIdentifier(options.RefreshTokensTableName, nameof(options.RefreshTokensTableName), failures);
        CheckIdentifier(options.MfaChallengesTableName, nameof(options.MfaChallengesTableName), failures);
        CheckIdentifier(options.RevocationsTableName, nameof(options.RevocationsTableName), failures);
        CheckIdentifier(options.ClaimKindsTableName, nameof(options.ClaimKindsTableName), failures);
        CheckIdentifier(options.ClaimsTableName, nameof(options.ClaimsTableName), failures);
        CheckIdentifier(options.UserClaimsTableName, nameof(options.UserClaimsTableName), failures);
        CheckIdentifier(options.RoleClaimsTableName, nameof(options.RoleClaimsTableName), failures);
        CheckIdentifier(options.UserRolesTableName, nameof(options.UserRolesTableName), failures);
    }

    private static void CheckIdentifier(
        string? value,
        string propertyName,
        List<string> failures)
    {
        if (!SqlIdentifierValidator.IsValid(value))
        {
            failures.Add($"{propertyName} must match ^[A-Za-z_][A-Za-z0-9_]*$.");
        }
    }
}
