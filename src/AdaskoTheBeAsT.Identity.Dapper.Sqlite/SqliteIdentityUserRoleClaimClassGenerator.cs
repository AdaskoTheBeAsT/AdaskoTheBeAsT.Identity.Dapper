using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

public class SqliteIdentityUserRoleClaimClassGenerator
    : IdentityUserRoleClaimClassGeneratorBase
{
    protected override DatabaseProvider Provider => DatabaseProvider.Sqlite;

    protected override string ProcessIdentityUserRoleClaimGetRoleClaimsByUserIdSql(IdentityDapperConfiguration config) =>
        Sql(config).GetRoleClaimsByUserId();

    protected override string ProcessIdentityUserRoleClaimGetUserAndRoleClaimsByUserIdSql(
        IdentityDapperConfiguration config) =>
        Sql(config).GetUserAndRoleClaimsByUserId();
}
