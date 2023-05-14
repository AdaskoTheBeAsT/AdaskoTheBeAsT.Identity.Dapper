using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserTokenClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserTokenClassGenerator
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
        GenerateClassStart(sb, "IdentityUserTokenSql", "IIdentityUserTokenSql");
        GenerateCreateSql(sb, config, combined);
        GenerateDeleteSql(sb, config);
        GenerateGetByUserIdSql(sb, config, combined);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserTokenCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserTokenDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserTokenGetByUserIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserTokenCreateSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserTokenDeleteSql(config);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserTokenGetByUserIdSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string GetByUserIdSql {{ get; }} =
            @""{content}"";");
    }

    private IList<string> GetStandardPropertyNames() =>
        typeof(IdentityUserToken<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();
}
