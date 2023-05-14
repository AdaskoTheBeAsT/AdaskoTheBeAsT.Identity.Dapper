using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public class IdentityClassGeneratorBase
{
    private const string Normalized = "Normalized";

    protected virtual void GenerateUsing(StringBuilder sb)
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

    protected IList<PropertyColumnPair> GetListWithoutNormalized(
        bool skipNormalized,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        if (!skipNormalized)
        {
            return propertyColumnPairs;
        }

        var newPairs = new List<PropertyColumnPair>();
        foreach (var propertyColumnPair in propertyColumnPairs)
        {
            if (IsNormalizedName(propertyColumnPair.ColumnName))
            {
                continue;
            }

            newPairs.Add(new PropertyColumnPair(propertyColumnPair.PropertyName, propertyColumnPair.ColumnName));
        }

        return newPairs;
    }

    protected IList<PropertyColumnPair> GetNormalizedSelectList(
        bool skipNormalized,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        if (!skipNormalized)
        {
            return propertyColumnPairs;
        }

        var newPairs = new List<PropertyColumnPair>();
        foreach (var propertyColumnPair in propertyColumnPairs)
        {
            var columnName = IsNormalizedName(propertyColumnPair.ColumnName)
                ? TrimNormalizedName(propertyColumnPair.ColumnName)
                : propertyColumnPair.ColumnName;
            newPairs.Add(
                new PropertyColumnPair(
                    propertyColumnPair.PropertyName,
                    columnName));
        }

        return newPairs;
    }

    protected IList<PropertyColumnPair> CombineStandardWithCustom(
        IEnumerable<string> propertyNames,
        IEnumerable<PropertyColumnPair> customs)
    {
        var dict = customs.ToDictionary(
            i => i.PropertyName,
            i => i.ColumnName,
            StringComparer.OrdinalIgnoreCase);
        var result = new List<PropertyColumnPair>();
        foreach (var propertyName in propertyNames)
        {
            result.Add(
                dict.TryGetValue(propertyName, out var columnName)
                    ? new PropertyColumnPair(propertyName, columnName)
                    : new PropertyColumnPair(propertyName, propertyName));
        }

        return result;
    }
}
