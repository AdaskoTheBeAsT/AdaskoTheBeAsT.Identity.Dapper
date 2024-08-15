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

    protected void GenerateNamespaceStart(StringBuilder sb, string namespaceName) =>
        sb.AppendLine(
            $@"namespace {namespaceName}
{{");

    protected void GenerateClassStart(StringBuilder sb, string className, string interfaceName) =>
        sb.AppendLine(
            $@"    public class {className}
        : {interfaceName}
    {{");

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
            if (IsNormalizedName(propertyColumnTypeTriple.ColumnName))
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
        return CombineStandardWithCustom(standardProperties, customs);
    }

    protected IList<PropertyColumnTypeTriple> CombineStandardWithCustom(
        IEnumerable<(string PropertyName, string PropertyType)> propertyInfos,
        IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var dict = customs.ToDictionary(
            i => i.PropertyName,
            i => new { i.PropertyType, i.ColumnName},
            StringComparer.OrdinalIgnoreCase);
        var result = new List<PropertyColumnTypeTriple>();
        foreach (var propertyInfo in propertyInfos)
        {
            result.Add(
                dict.TryGetValue(propertyInfo.PropertyName, out var info)
                    ? new PropertyColumnTypeTriple(propertyInfo.PropertyName, info.PropertyType, info.ColumnName)
                    : new PropertyColumnTypeTriple(propertyInfo.PropertyName, propertyInfo.PropertyType, propertyInfo.PropertyName));
        }

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
