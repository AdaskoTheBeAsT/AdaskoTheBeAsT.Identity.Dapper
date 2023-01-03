using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.SqlClient;

public static class SourceGenerationHelper
{
    public static void GenerateCode(
        SourceProductionContext context,
        (string? KeyTypeName, IList<(IPropertySymbol PropertySymbol, string ColumnName)> Items) classesToGenerate)
    {
        var grouped = classesToGenerate
            .Items
            .GroupBy<(IPropertySymbol, string), INamedTypeSymbol>(
                p => p.Item1.ContainingType,
                SymbolEqualityComparer.Default);

        foreach (var group in grouped)
        {
            ProcessClass(context, group.Key, group.ToList());
        }
    }

    private static void ProcessClass(
        SourceProductionContext context,
        INamedTypeSymbol classSymbol,
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> list)
    {
        switch (classSymbol.BaseType?.Name)
        {
            case "IdentityUser":
                ProcessIdentityUser(context, classSymbol, list);
                break;
            default:
                break;
        }
    }

    private static void ProcessIdentityUser(
        SourceProductionContext context,
        INamedTypeSymbol classSymbol,
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> list)
    {
        var sb = new StringBuilder();

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var columnNames = list.Select(i => i.ColumnName);
        var propertyNames = list.Select(i => i.PropertySymbol.Name);
        sb.AppendLine(
            $@"using AdaskoTheBeAsT.Identity.Dapper.IdentitySql;

namespace {namespaceName}
{{
    public class IdentityUserSql
        : IIdentityUserSql
    {{
        public string CreateSql {{ get; }} =
            @""{ProcessIdentityUserCreateSql(columnNames, propertyNames)}"";
    }}
}}");

        context.AddSource("IdentityUserSql.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static string ProcessIdentityUserCreateSql(
        IEnumerable<string> columnNames,
        IEnumerable<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\n,", columnNames))
            .Values(string.Join("\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                "INSERT INTO dbo.AspNetUsers(/**insert**/) \nVALUES(/**values**/);\nSELECT SCOPE_IDENTITY();")
            .RawSql;
    }
}
