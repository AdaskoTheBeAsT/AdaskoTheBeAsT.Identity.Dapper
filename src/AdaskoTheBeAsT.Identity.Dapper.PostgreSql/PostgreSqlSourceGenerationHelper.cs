using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;

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
            new PostgreSqlIdentityUserRoleClaimClassGenerator(),
            new PostgreSqlApplicationUserOnlyStoreGenerator(),
            new PostgreSqlApplicationUserStoreGenerator(),
            new PostgreSqlApplicationRoleStoreGenerator())
    {
    }

    protected override string GenerateSchemaPart(string dbSchema) =>
        string.IsNullOrEmpty(dbSchema) ? string.Empty : $"{dbSchema}.";

    protected override void GenerateAdditionalFiles(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        // noop
    }
}
