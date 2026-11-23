using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;
using Oracle.ManagedDataAccess.Client;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Oracle.Options;

public sealed class IdentityJwtOracleOptions
{
    public string? ConnectionString { get; set; }

    public Func<IServiceProvider, OracleConnection>? ConnectionFactory { get; set; }

    public string? SchemaName { get; set; }

    public string? IdentitySchemaName { get; set; }

    public string TablePrefix { get; set; } = "identityjwt";

    public string UserClaimsTableName { get; set; } = "ASPNETUSERCLAIMS";

    public string RoleClaimsTableName { get; set; } = "ASPNETROLECLAIMS";

    public string UserRolesTableName { get; set; } = "ASPNETUSERROLES";

    public Func<string, object>? IdentityUserIdConverter { get; set; }

    public bool IncludeRoleClaimsInTransformation { get; set; } = true;

    public bool EnableCleanupHostedService { get; set; } = true;

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(15);

    public int CleanupBatchSize { get; set; } = 500;

    internal RelationalOptions ToRelational()
    {
        var factory = ConnectionFactory;
        return new RelationalOptions
        {
            ConnectionString = ConnectionString,
            ConnectionFactory = factory == null ? null : provider => factory(provider),
            SchemaName = SchemaName,
            IdentitySchemaName = IdentitySchemaName,
            TablePrefix = TablePrefix,
            UserClaimsTableName = UserClaimsTableName,
            RoleClaimsTableName = RoleClaimsTableName,
            UserRolesTableName = UserRolesTableName,
            IdentityUserIdConverter = IdentityUserIdConverter,
            IncludeRoleClaimsInTransformation = IncludeRoleClaimsInTransformation,
            EnableCleanupHostedService = EnableCleanupHostedService,
            CleanupInterval = CleanupInterval,
            CleanupBatchSize = CleanupBatchSize,
        };
    }
}
