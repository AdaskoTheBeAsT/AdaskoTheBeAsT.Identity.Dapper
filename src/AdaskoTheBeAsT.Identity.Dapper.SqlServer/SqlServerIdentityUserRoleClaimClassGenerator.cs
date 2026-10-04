using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerIdentityUserRoleClaimClassGenerator
    : IdentityUserRoleClaimClassGeneratorBase
{
    protected override DatabaseProvider Provider => DatabaseProvider.SqlServer;

    protected override string ProcessIdentityUserRoleClaimGetRoleClaimsByUserIdSql(IdentityDapperConfiguration config) =>
        Sql(config).GetRoleClaimsByUserId();

    protected override string ProcessIdentityUserRoleClaimGetUserAndRoleClaimsByUserIdSql(
        IdentityDapperConfiguration config) =>
        Sql(config).GetUserAndRoleClaimsByUserId();
}
