using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class SourceGeneratorHelperBase
    : ISourceGeneratorHelper
{
    private readonly IIdentityUserSourceGenerator _identityUserSourceGenerator;

    protected SourceGeneratorHelperBase(IIdentityUserSourceGenerator identityUserSourceGenerator)
    {
        _identityUserSourceGenerator = identityUserSourceGenerator;
    }

    public void GenerateCode(
        SourceProductionContext context,
        (string? KeyTypeName, IList<(IPropertySymbol PropertySymbol, string ColumnName)> Items) generationInfo)
    {
        var grouped = generationInfo
            .Items
            .GroupBy<(IPropertySymbol, string), INamedTypeSymbol>(
                p => p.Item1.ContainingType,
                SymbolEqualityComparer.Default);

        foreach (var group in grouped)
        {
            ProcessClass(context, group.Key, group.ToList());
        }
    }

    private void ProcessClass(
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

    private void ProcessIdentityUser(
        SourceProductionContext context,
        INamedTypeSymbol classSymbol,
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> list)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var (propertyNames, columnNames) = Extract(list);
        var content = _identityUserSourceGenerator.Generate(namespaceName, propertyNames, columnNames);

        context.AddSource("IdentityUserSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private (IEnumerable<string> PropertyNames, IEnumerable<string> ColumnNames) Extract(
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> list) =>
        (list.Select(i => i.PropertySymbol.Name),
            list.Select(i => i.ColumnName));
}
