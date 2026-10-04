using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerIdentityRoleClaimClassGenerator
    : IdentityRoleClaimClassGeneratorBase
{
    protected override DatabaseProvider Provider => DatabaseProvider.SqlServer;

    protected override string ProcessIdentityRoleClaimCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        Sql(config, propertyColumnTypeTriples).Create();

    protected override string ProcessIdentityRoleClaimDeleteSql(IdentityDapperConfiguration config) =>
        Sql(config).DeleteEntity();

    protected override string ProcessIdentityRoleClaimGetByRoleIdSql(IdentityDapperConfiguration config) =>
        Sql(config).GetClaimsByOwnerId();
}
