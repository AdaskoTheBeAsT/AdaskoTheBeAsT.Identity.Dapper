using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public MySqlSourceGenerationHelper()
        : base(
            new MySqlIdentityRoleClassGenerator(),
            new MySqlIdentityRoleClaimClassGenerator(),
            new MySqlIdentityUserClassGenerator(),
            new MySqlIdentityUserClaimClassGenerator(),
            new MySqlIdentityUserLoginClassGenerator(),
            new MySqlIdentityUserRoleClassGenerator(),
            new MySqlIdentityUserTokenClassGenerator(),
            new MySqlApplicationUserOnlyStoreGenerator(),
            new MySqlApplicationUserStoreGenerator(),
            new MySqlApplicationRoleStoreGenerator())
    {
    }

    protected override string GenerateSchemaPart(string dbSchema) =>
        string.IsNullOrEmpty(dbSchema) ? string.Empty : $"`{dbSchema}`.";
}
