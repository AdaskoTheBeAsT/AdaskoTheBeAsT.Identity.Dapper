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

    public abstract IList<PropertyColumnTypeTriple> GetAllProperties(
                    IEnumerable<PropertyColumnTypeTriple> customs,
                    bool insertOwnId);

    internal MappedIdentitySql Sql(
                    IdentityDapperConfiguration config,
                    IList<PropertyColumnTypeTriple>? properties = null) =>
                    new(
                        config.ForGeneration(config.BaseTypeName, Provider, properties ?? Array.Empty<PropertyColumnTypeTriple>()),
                        properties ?? Array.Empty<PropertyColumnTypeTriple>(),
                        QuoteColumn,
                        ParameterPrefix);

    internal string AddConcurrencyPredicate(string sql, IdentityDapperConfiguration config, string parameter) =>
                    MappedIdentitySql.AppendConcurrencyPredicate(
                        sql, QuoteColumn(config.Column(config.BaseTypeName, "ConcurrencyStamp")), ParameterPrefix + parameter);

    // SQL classes intentionally retain the compact generated wrapper while store
    // generators keep the established multiline inheritance layout.
    protected static void GenerateSqlUsing(StringBuilder sb) =>
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper.Abstractions;");

    protected static void GenerateSqlClassStart(StringBuilder sb, string className, string interfaceName) =>
                    sb.Append("    public class ").Append(className).Append(" : ").Append(interfaceName).AppendLine("\n    {");

    protected static void GenerateNamespaceStart(StringBuilder sb, string namespaceName) =>
                    sb.Append("namespace ").Append(namespaceName).AppendLine("\r\n{");

    protected static void GenerateClassStart(StringBuilder sb, string className, string interfaceName) =>
                    sb.Append("    public class ").Append(className).Append("\r\n        : ").Append(interfaceName).AppendLine("\r\n    {");

    protected static void GenerateClassEnd(StringBuilder sb) =>
                    sb.AppendLine("    }");

    protected static void GenerateNamespaceEnd(StringBuilder sb) =>
                    sb.AppendLine("}");

    protected static bool IsNormalizedName(string name) =>
                    !string.IsNullOrEmpty(name) &&
                    name.IndexOf(Normalized, StringComparison.OrdinalIgnoreCase) >= 0;

    protected static string TrimNormalizedName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return name
            .Replace(Normalized, string.Empty)
            .Replace(Normalized.ToLowerInvariant(), string.Empty);
    }

    protected static IList<PropertyColumnTypeTriple> GetListWithoutNormalized(
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

    protected static IList<PropertyColumnTypeTriple> CombineStandardWithCustom(
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

    protected static IList<(string PropertyName, string PropertyType)> GetStandardProperties(
                    Type type,
                    bool insertOwnId) =>
                    type
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(
                            p => ((type == typeof(IdentityRole<>) || type == typeof(IdentityUser<>)) && insertOwnId) ||
                                 !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                        .Select(p => (PropertyName: p.Name, PropertyType: p.PropertyType.Name))
                        .ToList();

    protected static IList<PropertyColumnTypeTriple> GetNormalizedSelectList(
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

    protected static IList<PropertyColumnTypeTriple> GetStandardWithCombinedProperties(
                    Type type,
                    bool insertOwnId,
                    IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var standardProperties = GetStandardProperties(type, insertOwnId);
        return CombineStandardWithCustom(standardProperties, customs)
            .Where(p => !string.IsNullOrEmpty(p.ColumnName)).ToList();
    }

    protected virtual string QuoteColumn(string column) => MappedIdentitySql.QuoteIdentifier(Provider, column);

    protected string AddConcurrencyPredicate(
                    string sql,
                    IList<PropertyColumnTypeTriple> properties,
                    string parameter)
    {
        var column = QuoteColumn(properties.First(p => string.Equals(p.PropertyName, "ConcurrencyStamp", StringComparison.Ordinal)).ColumnName);
        var value = ParameterPrefix + parameter;
        return MappedIdentitySql.AppendConcurrencyPredicate(sql, column, value);
    }

    protected virtual void GenerateUsing(
                    StringBuilder sb,
                    string keyTypeName)
    {
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper.Abstractions;");
        sb.AppendLine();
    }
}
