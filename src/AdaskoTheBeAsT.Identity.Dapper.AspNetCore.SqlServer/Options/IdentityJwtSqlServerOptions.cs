using System;
using Microsoft.Data.SqlClient;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;

public sealed class IdentityJwtSqlServerOptions
{
    public string? ConnectionString { get; set; }

    public Func<IServiceProvider, SqlConnection>? ConnectionFactory { get; set; }

    public string SchemaName { get; set; } = "dbo";

    public string? IdentitySchemaName { get; set; }

    public string RefreshTokensTableName { get; set; } = "IdentityJwtRefreshTokens";

    public string MfaChallengesTableName { get; set; } = "IdentityJwtMfaChallenges";

    public string RevocationsTableName { get; set; } = "IdentityJwtRevocations";

    public string ClaimKindsTableName { get; set; } = "AspNetClaimKinds";

    public string ClaimsTableName { get; set; } = "AspNetClaims";

    public string UserClaimsTableName { get; set; } = "AspNetUserClaims";

    public string RoleClaimsTableName { get; set; } = "AspNetRoleClaims";

    public string UserRolesTableName { get; set; } = "AspNetUserRoles";

    public bool IncludeRoleClaimsInTransformation { get; set; } = true;

    public bool EnableCleanupHostedService { get; set; } = true;

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(15);

    public int CleanupBatchSize { get; set; } = 500;
}
