using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public SqlSourceGenerationHelper()
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
}
