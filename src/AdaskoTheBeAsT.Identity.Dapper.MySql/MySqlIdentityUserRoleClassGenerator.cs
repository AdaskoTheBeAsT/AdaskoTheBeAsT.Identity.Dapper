using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlIdentityUserRoleClassGenerator
    : IdentityUserRoleClassGeneratorBase
{
    protected override DatabaseProvider Provider => DatabaseProvider.MySql;

    protected override string ProcessIdentityUserRoleCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        Sql(config, propertyColumnTypeTriples).Create();

    protected override string ProcessIdentityUserRoleDeleteSql(IdentityDapperConfiguration config) =>
        Sql(config).DeleteEntity();

    protected override string ProcessIdentityUserRoleGetByUserIdRoleIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        Sql(config, propertyColumnTypeTriples).GetByUserIdRoleId();

    protected override string ProcessIdentityUserRoleGetCount(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        Sql(config, propertyColumnTypeTriples).GetCount();

    protected override string ProcessIdentityUserRoleGetRoleNamesByUserId(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        Sql(config, propertyColumnTypeTriples).GetRoleNamesByUserId();
}
