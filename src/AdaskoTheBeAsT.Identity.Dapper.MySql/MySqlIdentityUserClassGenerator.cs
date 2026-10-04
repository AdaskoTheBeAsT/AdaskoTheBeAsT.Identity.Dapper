using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlIdentityUserClassGenerator
    : IdentityUserClassGeneratorBase
{
    public MySqlIdentityUserClassGenerator()
    {
    }

    protected override DatabaseProvider Provider => DatabaseProvider.MySql;

    protected override string ProcessIdentityUserCreateSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).Create();

    protected override string ProcessIdentityUserUpdateSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).UpdateEntity();

    protected override string ProcessIdentityUserDeleteSql(IdentityDapperConfiguration config) =>
            Sql(config).DeleteEntity();

    protected override string ProcessIdentityUserFindByIdSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).FindById();

    protected override string ProcessIdentityUserFindByNameSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).FindByName();

    protected override string ProcessIdentityUserFindByEmailSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).FindByEmail();

    protected override string ProcessIdentityUserGetUsersForClaimSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).GetUsersForClaim();

    protected override string ProcessIdentityUserGetUsersInRoleSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).GetUsersInRole();

    protected override string ProcessIdentityUserGetUsersSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            Sql(config, propertyColumnTypeTriples).GetAll();
}
