using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserLoginClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserLoginClassGenerator
{
    public string Generate(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var sb = new StringBuilder();
        GenerateUsing(sb, config.KeyTypeName);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateClassStart(sb, "IdentityUserLoginSql", "IIdentityUserLoginSql");
        GenerateCreateSql(sb, config.SchemaPart, propertyColumnTypeTriples);
        GenerateDeleteSql(sb, config.SchemaPart);
        GenerateGetByUserIdSql(sb, config.SchemaPart, propertyColumnTypeTriples);
        GenerateGetByUserIdLoginProviderKeySql(sb, config.SchemaPart, propertyColumnTypeTriples);
        GenerateGetByLoginProviderKeySql(sb, config.SchemaPart, propertyColumnTypeTriples);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    public override IList<PropertyColumnTypeTriple> GetAllProperties(
        IEnumerable<PropertyColumnTypeTriple> customs,
        bool insertOwnId) =>
        GetStandardWithCombinedProperties(typeof(IdentityUserLogin<>), insertOwnId, customs);

    protected abstract string ProcessIdentityUserLoginCreateSql(
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserLoginDeleteSql(string schemaPart);

    protected abstract string ProcessIdentityUserLoginGetByUserIdSql(
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserLoginGetByLoginProviderKeySql(
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    private void GenerateCreateSql(
        StringBuilder sb,
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserLoginCreateSql(schemaPart, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        string schemaPart)
    {
        var content = ProcessIdentityUserLoginDeleteSql(schemaPart);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdSql(
        StringBuilder sb,
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserLoginGetByUserIdSql(schemaPart, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string GetByUserIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdLoginProviderKeySql(
        StringBuilder sb,
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(schemaPart, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string GetByUserIdLoginProviderKeySql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByLoginProviderKeySql(
        StringBuilder sb,
        string schemaPart,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserLoginGetByLoginProviderKeySql(schemaPart, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string GetByLoginProviderKeySql {{ get; }} =
            @""{content}"";");
    }
}
