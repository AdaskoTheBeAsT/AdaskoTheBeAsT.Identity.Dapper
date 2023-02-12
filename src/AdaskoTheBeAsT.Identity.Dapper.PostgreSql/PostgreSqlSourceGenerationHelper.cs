using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

public class PostgreSqlSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public PostgreSqlSourceGenerationHelper()
        : base(
            new PostgreSqlIdentityRoleClassGenerator(),
            new PostgreSqlIdentityRoleClaimClassGenerator(),
            new PostgreSqlIdentityUserClassGenerator(),
            new PostgreSqlIdentityUserClaimClassGenerator(),
            new PostgreSqlIdentityUserLoginClassGenerator(),
            new PostgreSqlIdentityUserRoleClassGenerator(),
            new PostgreSqlIdentityUserTokenClassGenerator(),
            new PostgreSqlApplicationUserOnlyStoreGenerator(),
            new PostgreSqlApplicationUserStoreGenerator(),
            new PostgreSqlApplicationRoleStoreGenerator())
    {
    }

    protected override string GenerateSchemaPart(string dbSchema) =>
        string.IsNullOrEmpty(dbSchema) ? string.Empty : $"{dbSchema}.";
}
