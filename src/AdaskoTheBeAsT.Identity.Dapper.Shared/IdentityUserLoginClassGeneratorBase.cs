using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        IEnumerable<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var standardProperties = GetStandardProperties();
        var combined = CombineStandardWithCustom(standardProperties, propertyColumnTypeTriples);

        var sb = new StringBuilder();
        GenerateUsing(sb, config.KeyTypeName);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateClassStart(sb, "IdentityUserLoginSql", "IIdentityUserLoginSql");
        GenerateCreateSql(sb, config.SchemaPart, combined);
        GenerateDeleteSql(sb, config.SchemaPart);
        GenerateGetByUserIdSql(sb, config.SchemaPart, combined);
        GenerateGetByUserIdLoginProviderKeySql(sb, config.SchemaPart, combined);
        GenerateGetByLoginProviderKeySql(sb, config.SchemaPart, combined);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

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

    private IList<(string PropertyName, string PropertyType)> GetStandardProperties() =>
        typeof(IdentityUserLogin<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => (PropertyName: p.Name, PropertyType: p.PropertyType.Name))
            .ToList();
}
