using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public SqlServerSourceGenerationHelper()
        : base(
            new SqlServerIdentityRoleClassGenerator(),
            new SqlServerIdentityRoleClaimClassGenerator(),
            new SqlServerIdentityUserClassGenerator(),
            new SqlServerIdentityUserClaimClassGenerator(),
            new SqlServerIdentityUserLoginClassGenerator(),
            new SqlServerIdentityUserRoleClassGenerator(),
            new SqlServerIdentityUserTokenClassGenerator(),
            new SqlServerApplicationUserOnlyStoreGenerator(),
            new SqlServerApplicationUserStoreGenerator(),
            new SqlServerApplicationRoleStoreGenerator())
    {
    }

    protected override string GenerateSchemaPart(string dbSchema) =>
        string.IsNullOrEmpty(dbSchema) ? string.Empty : $"{dbSchema}.";
}
