using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityClassGeneratorBase
    : IIdentityClassGeneratorBase
{
    private const string Normalized = "Normalized";

    protected virtual DatabaseProvider Provider => DatabaseProvider.SqlServer;

    protected virtual string ParameterPrefix => Provider == DatabaseProvider.Oracle ? ":" : "@";

    protected virtual string QuoteColumn(string column) => MappedIdentitySql.QuoteIdentifier(Provider, column);

    internal MappedIdentitySql Sql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple>? properties = null) =>
        new(
            config.ForGeneration(config.BaseTypeName, Provider, properties ?? Array.Empty<PropertyColumnTypeTriple>()),
            properties ?? Array.Empty<PropertyColumnTypeTriple>(),
            QuoteColumn,
            ParameterPrefix);

    protected string AddConcurrencyPredicate(
        string sql,
        IList<PropertyColumnTypeTriple> properties,
        string parameter)
    {
        var column = QuoteColumn(properties.First(p => p.PropertyName == "ConcurrencyStamp").ColumnName);
        var value = ParameterPrefix + parameter;
        return MappedIdentitySql.AppendConcurrencyPredicate(sql, column, value);
    }

    internal string AddConcurrencyPredicate(string sql, IdentityDapperConfiguration config, string parameter) =>
        MappedIdentitySql.AppendConcurrencyPredicate(
            sql, QuoteColumn(config.Column(config.BaseTypeName, "ConcurrencyStamp")), ParameterPrefix + parameter);

    public abstract IList<PropertyColumnTypeTriple> GetAllProperties(
        IEnumerable<PropertyColumnTypeTriple> customs,
        bool insertOwnId);

    protected virtual void GenerateUsing(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper.Abstractions;");
        sb.AppendLine();
    }

    // SQL classes intentionally retain the compact generated wrapper while store
    // generators keep the established multiline inheritance layout.
    protected void GenerateSqlUsing(StringBuilder sb) =>
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper.Abstractions;");

    protected void GenerateSqlClassStart(StringBuilder sb, string className, string interfaceName) =>
        sb.AppendLine($"    public class {className} : {interfaceName}\n    {{");

    protected void GenerateNamespaceStart(StringBuilder sb, string namespaceName) =>
        sb.AppendLine(
            $$"""
            namespace {{namespaceName}}
            {
            """);

    protected void GenerateClassStart(StringBuilder sb, string className, string interfaceName) =>
        sb.AppendLine(
            $$"""
                public class {{className}}
                    : {{interfaceName}}
                {
            """);

    protected void GenerateClassEnd(StringBuilder sb) =>
        sb.AppendLine("    }");

    protected void GenerateNamespaceEnd(StringBuilder sb) =>
        sb.AppendLine("}");

    protected bool IsNormalizedName(string name) =>
        !string.IsNullOrEmpty(name) &&
        name.IndexOf(Normalized, StringComparison.OrdinalIgnoreCase) >= 0;

    protected string TrimNormalizedName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return name
            .Replace(Normalized, string.Empty)
            .Replace(Normalized.ToLowerInvariant(), string.Empty);
    }

    protected IList<PropertyColumnTypeTriple> GetListWithoutNormalized(
        bool skipNormalized,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        if (!skipNormalized)
        {
            return propertyColumnTypeTriples;
        }

        var newPairs = new List<PropertyColumnTypeTriple>();
        foreach (var propertyColumnTypeTriple in propertyColumnTypeTriples)
        {
            if (propertyColumnTypeTriple.PropertyName is "NormalizedUserName" or "NormalizedEmail" or "NormalizedName")
            {
                continue;
            }

            newPairs.Add(
                new PropertyColumnTypeTriple(
                    propertyColumnTypeTriple.PropertyName,
                    propertyColumnTypeTriple.PropertyType,
                    propertyColumnTypeTriple.ColumnName));
        }

        return newPairs;
    }

    protected IList<PropertyColumnTypeTriple> GetNormalizedSelectList(
        bool skipNormalized,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        if (!skipNormalized)
        {
            return propertyColumnTypeTriples;
        }

        var newPairs = new List<PropertyColumnTypeTriple>();
        foreach (var propertyColumnTypeTriple in propertyColumnTypeTriples)
        {
            var columnName = IsNormalizedName(propertyColumnTypeTriple.ColumnName)
                ? TrimNormalizedName(propertyColumnTypeTriple.ColumnName)
                : propertyColumnTypeTriple.ColumnName;
            newPairs.Add(
                new PropertyColumnTypeTriple(
                    propertyColumnTypeTriple.PropertyName,
                    propertyColumnTypeTriple.PropertyType,
                    columnName));
        }

        return newPairs;
    }

    protected IList<PropertyColumnTypeTriple> GetStandardWithCombinedProperties(
        Type type,
        bool insertOwnId,
        IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var standardProperties = GetStandardProperties(type, insertOwnId);
        return CombineStandardWithCustom(standardProperties, customs)
            .Where(p => !string.IsNullOrEmpty(p.ColumnName)).ToList();
    }

    protected IList<PropertyColumnTypeTriple> CombineStandardWithCustom(
        IEnumerable<(string PropertyName, string PropertyType)> propertyInfos,
        IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var customProperties = customs.ToList();
        var dict = customProperties.ToDictionary(
            i => i.PropertyName,
            i => new { i.PropertyType, i.ColumnName },
            StringComparer.OrdinalIgnoreCase);
        var result = new List<PropertyColumnTypeTriple>();
        foreach (var propertyInfo in propertyInfos)
        {
            result.Add(
                dict.TryGetValue(propertyInfo.PropertyName, out var info)
                    ? new PropertyColumnTypeTriple(propertyInfo.PropertyName, info.PropertyType, info.ColumnName)
                    : new PropertyColumnTypeTriple(propertyInfo.PropertyName, propertyInfo.PropertyType, propertyInfo.PropertyName));
        }

        var standardNames = new HashSet<string>(result.Select(p => p.PropertyName), StringComparer.OrdinalIgnoreCase);
        result.AddRange(customProperties.Where(p => !standardNames.Contains(p.PropertyName) &&
            !p.PropertyName.Equals("Id", StringComparison.OrdinalIgnoreCase)));

        return result;
    }

    protected IList<(string PropertyName, string PropertyType)> GetStandardProperties(
        Type type,
        bool insertOwnId) =>
        type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(
                p => ((type == typeof(IdentityRole<>) || type == typeof(IdentityUser<>)) && insertOwnId) ||
                     !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .Select(p => (PropertyName: p.Name, PropertyType: p.PropertyType.Name))
            .ToList();
}
