using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlIdentityUserRoleClaimClassGenerator
    : IdentityUserRoleClaimClassGeneratorBase
{
    protected override DatabaseProvider Provider => DatabaseProvider.MySql;

    protected override string ProcessIdentityUserRoleClaimGetRoleClaimsByUserIdSql(IdentityDapperConfiguration config) =>
        Sql(config).GetRoleClaimsByUserId();

    protected override string ProcessIdentityUserRoleClaimGetUserAndRoleClaimsByUserIdSql(
        IdentityDapperConfiguration config) =>
        Sql(config).GetUserAndRoleClaimsByUserId();
}
