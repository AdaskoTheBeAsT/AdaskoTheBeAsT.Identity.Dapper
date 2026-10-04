using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

public class SqliteIdentityRoleClassGenerator
    : IdentityRoleClassGeneratorBase
{
    public SqliteIdentityRoleClassGenerator()
    {
    }

    protected override DatabaseProvider Provider => DatabaseProvider.Sqlite;

    protected override string ProcessIdentityRoleCreateSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).Create();

    protected override string ProcessIdentityRoleUpdateSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).UpdateEntity();

    protected override string ProcessIdentityRoleDeleteSql(IdentityDapperConfiguration config) =>
            Sql(config).DeleteEntity();

    protected override string ProcessIdentityRoleFindByIdSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).FindById();

    protected override string ProcessIdentityRoleFindByNameSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).FindByName();

    protected override string ProcessIdentityRoleGetRolesSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).GetAll();
}
