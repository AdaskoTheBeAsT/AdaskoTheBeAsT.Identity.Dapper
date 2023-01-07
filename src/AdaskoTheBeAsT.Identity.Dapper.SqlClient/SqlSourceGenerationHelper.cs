using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlClient;

public class SqlSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public SqlSourceGenerationHelper()
        : base(
            new SqlIdentityRoleClassGenerator(),
            new SqlIdentityRoleClaimClassGenerator(),
            new SqlIdentityUserClassGenerator(),
            new SqlIdentityUserClaimClassGenerator(),
            new SqlIdentityUserLoginClassGenerator(),
            new SqlIdentityUserRoleClassGenerator(),
            new SqlIdentityUserTokenClassGenerator())
    {
    }
}
