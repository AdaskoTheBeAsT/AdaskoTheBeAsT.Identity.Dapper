using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.SqlClient;

public class IdentitySyntaxReceiver
    : ISyntaxContextReceiver
{
    public IList<(IPropertySymbol PropertySymbol, string ColumnName)> IdentityPropertiesSymbol { get; } =
        new List<(IPropertySymbol, string)>();

    public string? KeyTypeName { get; set; }

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        if (classDeclarationSyntax.BaseList is null)
        {
            return;
        }

        var types = classDeclarationSyntax.BaseList
            .Types
            .Select(t => t.Type);

        var identityClass = types.Where(
                t => t is GenericNameSyntax { Identifier.Value: { } })
            .Cast<GenericNameSyntax>()
            .FirstOrDefault(
                s => s.Identifier.ValueText.StartsWith(nameof(Identity), StringComparison.OrdinalIgnoreCase));

        if (identityClass == null)
        {
            return;
        }

        ProcessIdentityClass(context, classDeclarationSyntax, identityClass);
    }

    private void ProcessIdentityClass(
        GeneratorSyntaxContext context,
        ClassDeclarationSyntax classDeclarationSyntax,
        GenericNameSyntax identityClass)
    {
        var keyTypeName = identityClass.TypeArgumentList.Arguments.Any()
            ? (identityClass.TypeArgumentList.Arguments[0] as IdentifierNameSyntax)?.Identifier.ValueText ??
              "string"
            : "string";

        if (KeyTypeName?.Equals(keyTypeName, StringComparison.OrdinalIgnoreCase) == false)
        {
            throw new KeyTypeNotSameException();
        }

        KeyTypeName = keyTypeName;

        foreach (var memberDeclarationSyntax in classDeclarationSyntax.Members)
        {
            if (memberDeclarationSyntax is not PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                continue;
            }

            var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);

            if (propertySymbol is null)
            {
                continue;
            }

            ProcessProperty(propertySymbol, propertyDeclarationSyntax);
        }
    }

    private void ProcessProperty(
        IPropertySymbol propertySymbol,
        PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        var columnName = propertySymbol.Name;

        var attributeListSyntax = propertyDeclarationSyntax
            .AttributeLists
            .FirstOrDefault(
                al => al.Attributes.Any(
                    a => (a.Name as IdentifierNameSyntax)?.Identifier.ValueText.Contains("Column") ??
                         false));

        var attributes = attributeListSyntax?.Attributes.FirstOrDefault(
            a => (a.Name as IdentifierNameSyntax)?.Identifier.ValueText.Contains("Column") ?? false);
        if (attributes?.ArgumentList?.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax
            literalExpressionSyntax)
        {
            columnName = literalExpressionSyntax.Token.ValueText;
        }

        IdentityPropertiesSymbol.Add((PropertySymbol: propertySymbol, ColumnName: columnName));
    }
}
