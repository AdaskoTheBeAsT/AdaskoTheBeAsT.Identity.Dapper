using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

public class SqliteSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public SqliteSourceGenerationHelper()
        : base(
            new SqliteIdentityRoleClassGenerator(),
            new SqliteIdentityRoleClaimClassGenerator(),
            new SqliteIdentityUserClassGenerator(),
            new SqliteIdentityUserClaimClassGenerator(),
            new SqliteIdentityUserLoginClassGenerator(),
            new SqliteIdentityUserRoleClassGenerator(),
            new SqliteIdentityUserTokenClassGenerator(),
            new SqliteApplicationUserOnlyStoreGenerator(),
            new SqliteApplicationUserStoreGenerator(),
            new SqliteApplicationRoleStoreGenerator())
    {
    }

    protected override string GenerateSchemaPart(string dbSchema) => string.Empty;
}
