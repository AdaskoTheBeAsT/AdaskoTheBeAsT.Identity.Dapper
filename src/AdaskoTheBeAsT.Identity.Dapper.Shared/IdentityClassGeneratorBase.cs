using System;
using System.Collections.Generic;
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

    protected (IList<string> ColumnNames, IList<string> PropertyNames) GetListWithoutNormalized(
        bool skipNormalized,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        if (!skipNormalized)
        {
            return (columnNames, propertyNames);
        }

        var localColumnNames = new List<string>();
        var localPropertyNames = new List<string>();

        for (var i = 0; i < columnNames.Count; i++)
        {
            var columnName = columnNames[i];
            if (IsNormalizedName(columnName))
            {
                continue;
            }

            localColumnNames.Add(columnName);
            localPropertyNames.Add(propertyNames[i]);
        }

        return (localColumnNames, localPropertyNames);
    }

    protected (IList<string> ColumnNames, IList<string> PropertyNames) GetNormalizedSelectList(
        bool skipNormalized,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        if (!skipNormalized)
        {
            return (columnNames, propertyNames);
        }

        var localColumnNames = new List<string>();
        var localPropertyNames = new List<string>();

        for (var i = 0; i < columnNames.Count; i++)
        {
            var columnName = columnNames[i];
            localColumnNames.Add(IsNormalizedName(columnName) ? TrimNormalizedName(columnName) : columnName);
            localPropertyNames.Add(propertyNames[i]);
        }

        return (localColumnNames, localPropertyNames);
    }
}
