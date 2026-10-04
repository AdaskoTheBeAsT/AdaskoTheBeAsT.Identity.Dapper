using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserTokenClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserTokenClassGenerator
{
    protected virtual string TokenTableName =>
        Provider is DatabaseProvider.MySql or DatabaseProvider.PostgreSql ? "aspnetusertokens" : "AspNetUserTokens";

    public string Generate(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        config = config.ForGeneration("IdentityUserToken", Provider, propertyColumnTypeTriples);
        var sb = new StringBuilder();
        GenerateSqlUsing(sb);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateSqlClassStart(sb, "IdentityUserTokenSql", "IIdentityUserTokenConcurrencySql");
        GenerateCreateSql(sb, config, propertyColumnTypeTriples);
        GenerateUpdateSql(sb, config, propertyColumnTypeTriples);
        GenerateDeleteSql(sb, config);
        GenerateGetByUserIdSql(sb, config, propertyColumnTypeTriples);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    public override IList<PropertyColumnTypeTriple> GetAllProperties(
        IEnumerable<PropertyColumnTypeTriple> customs,
        bool insertOwnId) =>
        GetStandardWithCombinedProperties(typeof(IdentityUserToken<>), insertOwnId, customs);

    protected abstract string ProcessIdentityUserTokenCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserTokenDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserTokenGetByUserIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    private void GenerateUpdateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> properties)
    {
        var content = Sql(config, properties).UpdateToken(TokenTableName);
        sb.Append("        public string UpdateSql { get; } =\r\n            ").Append(RawStringLiteral.Format(content)).Append(';').AppendLine();
        sb.AppendLine();
    }

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserTokenCreateSql(config, propertyColumnTypeTriples);
        sb.Append("        public string CreateSql { get; } =\r\n            ").Append(RawStringLiteral.Format(content)).Append(';').AppendLine();
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserTokenDeleteSql(config);
        sb.Append("        public string DeleteSql { get; } =\r\n            ").Append(RawStringLiteral.Format(content)).Append(';').AppendLine();
        sb.AppendLine();
    }

    private void GenerateGetByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserTokenGetByUserIdSql(config, propertyColumnTypeTriples);
        sb.Append("        public string GetByUserIdSql { get; } =\r\n            ").Append(RawStringLiteral.Format(content)).Append(';').AppendLine();
    }
}
