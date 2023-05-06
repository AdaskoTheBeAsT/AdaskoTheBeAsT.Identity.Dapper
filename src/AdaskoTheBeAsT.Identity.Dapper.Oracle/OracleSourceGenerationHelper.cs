using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public OracleSourceGenerationHelper()
        : base(
            new OracleIdentityRoleClassGenerator(),
            new OracleIdentityRoleClaimClassGenerator(),
            new OracleIdentityUserClassGenerator(),
            new OracleIdentityUserClaimClassGenerator(),
            new OracleIdentityUserLoginClassGenerator(),
            new OracleIdentityUserRoleClassGenerator(),
            new OracleIdentityUserTokenClassGenerator(),
            new OracleIdentityUserRoleClaimClassGenerator(),
            new OracleApplicationUserOnlyStoreGenerator(),
            new OracleApplicationUserStoreGenerator(),
            new OracleApplicationRoleStoreGenerator())
    {
    }

    protected override string GenerateSchemaPart(string dbSchema) =>
        string.IsNullOrEmpty(dbSchema) ? string.Empty : $"{dbSchema}.";
}
