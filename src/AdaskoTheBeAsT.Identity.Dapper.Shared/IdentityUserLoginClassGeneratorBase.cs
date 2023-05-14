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
        IEnumerable<PropertyColumnPair> propertyColumnPairs)
    {
        var standardProperties = GetStandardPropertyNames();
        var combined = CombineStandardWithCustom(standardProperties, propertyColumnPairs);

        var sb = new StringBuilder();
        GenerateUsing(sb);
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
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserLoginDeleteSql(string schemaPart);

    protected abstract string ProcessIdentityUserLoginGetByUserIdSql(
        string schemaPart,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(
        string schemaPart,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserLoginGetByLoginProviderKeySql(
        string schemaPart,
        IList<PropertyColumnPair> propertyColumnPairs);

    private void GenerateCreateSql(
        StringBuilder sb,
        string schemaPart,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserLoginCreateSql(schemaPart, propertyColumnPairs);
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
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserLoginGetByUserIdSql(schemaPart, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string GetByUserIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdLoginProviderKeySql(
        StringBuilder sb,
        string schemaPart,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(schemaPart, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string GetByUserIdLoginProviderKeySql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByLoginProviderKeySql(
        StringBuilder sb,
        string schemaPart,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserLoginGetByLoginProviderKeySql(schemaPart, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string GetByLoginProviderKeySql {{ get; }} =
            @""{content}"";");
    }

    private IList<string> GetStandardPropertyNames() =>
        typeof(IdentityUserLogin<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();
}
