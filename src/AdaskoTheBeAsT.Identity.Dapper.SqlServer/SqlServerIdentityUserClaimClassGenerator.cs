using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerIdentityUserClaimClassGenerator
    : IdentityUserClaimClassGeneratorBase
{
    protected override DatabaseProvider Provider => DatabaseProvider.SqlServer;

    protected override string ProcessIdentityUserClaimCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        Sql(config, propertyColumnTypeTriples).Create();

    protected override string ProcessIdentityUserClaimDeleteSql(IdentityDapperConfiguration config) =>
        Sql(config).DeleteEntity();

    protected override string ProcessIdentityUserClaimGetByUserIdSql(IdentityDapperConfiguration config) =>
        Sql(config).GetClaimsByOwnerId();

    protected override string ProcessIdentityUserClaimReplaceSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        Sql(config, propertyColumnTypeTriples).ReplaceClaim();
}
