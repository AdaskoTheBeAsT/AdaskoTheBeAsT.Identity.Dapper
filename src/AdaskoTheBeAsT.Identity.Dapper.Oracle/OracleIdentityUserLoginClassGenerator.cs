using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleIdentityUserLoginClassGenerator
    : IdentityUserLoginClassGeneratorBase
{
    protected override DatabaseProvider Provider => DatabaseProvider.Oracle;

    protected override string ProcessIdentityUserLoginCreateSql(
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        LoginSql(schemaPart, propertyColumnTypeTriples).Create();

    protected override string ProcessIdentityUserLoginDeleteSql(string schemaPart) =>
        LoginSql(schemaPart).DeleteEntity();

    protected override string ProcessIdentityUserLoginGetByUserIdSql(
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        LoginSql(schemaPart, propertyColumnTypeTriples).GetLoginByUserId();

    protected override string ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        LoginSql(schemaPart, propertyColumnTypeTriples).GetByUserIdLoginProviderKey();

    protected override string ProcessIdentityUserLoginGetByLoginProviderKeySql(
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
        LoginSql(schemaPart, propertyColumnTypeTriples).GetByLoginProviderKey();
}
