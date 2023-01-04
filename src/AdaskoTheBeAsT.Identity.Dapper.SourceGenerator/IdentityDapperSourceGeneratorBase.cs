using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityDapperSourceGeneratorBase
{
    private const string Column = "Column";
    private const string DefaultStringTypeName = "string";
    private readonly ISourceGeneratorHelper _sourceGeneratorHelper;

    protected IdentityDapperSourceGeneratorBase(
        ISourceGeneratorHelper sourceGeneratorHelper)
    {
        _sourceGeneratorHelper = sourceGeneratorHelper;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations =
            context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: static (
                        s,
                        _) => IsSyntaxTargetForGeneration(s),
                    transform: static (
                        ctx,
                        _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)
                .Select(
                    static (
                        c,
                        _) => c!);

        var compilationAndClasses =
            context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(
            compilationAndClasses,
            (
                    spc,
                    source) =>
                Execute(source.Left, source.Right, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax { BaseList: { } } classDeclaration
           && classDeclaration.BaseList.Types
               .Select(t => t.Type).Where(
                   t => t is GenericNameSyntax { Identifier.Value: { } })
               .Cast<GenericNameSyntax>()
               .Any(
                   s => s.Identifier.ValueText.StartsWith(nameof(Identity), StringComparison.OrdinalIgnoreCase));

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context) =>
        context.Node as ClassDeclarationSyntax;

    private void Execute(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations,
        SourceProductionContext context)
    {
        if (classDeclarations.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        // I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
        var distinctClassDeclarations = classDeclarations.Distinct();

        // Convert each EnumDeclarationSyntax to an EnumToGenerate
        var classesToGenerate = GetTypesToGenerate(
            compilation,
            distinctClassDeclarations,
            context.CancellationToken);

        // If there were errors in the EnumDeclarationSyntax, we won't create an
        // EnumToGenerate for it, so make sure we have something to generate
        if (classesToGenerate.Items.Count > 0)
        {
            // generate the source code and add it to the output
            _sourceGeneratorHelper.GenerateCode(context, classesToGenerate);
        }
    }

    private (string? KeyTypeName, IList<(IPropertySymbol PropertySymbol, string ColumnName)> Items) GetTypesToGenerate(
        Compilation compilation,
        IEnumerable<ClassDeclarationSyntax>? distinctClassDeclarations,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var identityPropertiesSymbol = new List<(IPropertySymbol PropertySymbol, string ColumnName)>();
        if (distinctClassDeclarations == null)
        {
            return (null, identityPropertiesSymbol);
        }

        string? keyTypeName = null;

        foreach (var classDeclarationSyntax in distinctClassDeclarations)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (classDeclarationSyntax.BaseList is null)
            {
                continue;
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
                continue;
            }

            var localKeyType = ProcessIdentityClass(semanticModel, classDeclarationSyntax, identityClass, identityPropertiesSymbol);
            if (keyTypeName?.Equals(localKeyType, StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new KeyTypeNotSameException();
            }

            keyTypeName = localKeyType;
        }

        return (keyTypeName, identityPropertiesSymbol);
    }

    private string ProcessIdentityClass(
        SemanticModel semanticModel,
        ClassDeclarationSyntax classDeclarationSyntax,
        GenericNameSyntax identityClass,
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> identityPropertiesSymbol)
    {
        var keyTypeName = identityClass.TypeArgumentList.Arguments.Any()
            ? (identityClass.TypeArgumentList.Arguments[0] as IdentifierNameSyntax)?.Identifier.ValueText ??
              DefaultStringTypeName
            : DefaultStringTypeName;

        foreach (var memberDeclarationSyntax in classDeclarationSyntax.Members)
        {
            if (memberDeclarationSyntax is not PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                continue;
            }

            if (semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax) is not IPropertySymbol propertySymbol)
            {
                continue;
            }

            ProcessProperty(identityPropertiesSymbol, propertySymbol, propertyDeclarationSyntax);
        }

        return keyTypeName;
    }

    private void ProcessProperty(
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> identityPropertiesSymbol,
        IPropertySymbol propertySymbol,
        PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        var columnName = propertySymbol.Name;

        var attributeListSyntax = propertyDeclarationSyntax
            .AttributeLists
            .FirstOrDefault(
                al => al.Attributes.Any(
                    a => (a.Name as IdentifierNameSyntax)?.Identifier.ValueText.Contains(Column) ??
                         false));

        var attributes = attributeListSyntax?.Attributes.FirstOrDefault(
            a => (a.Name as IdentifierNameSyntax)?.Identifier.ValueText.Contains(Column) ?? false);
        if (attributes?.ArgumentList?.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax
            literalExpressionSyntax)
        {
            columnName = literalExpressionSyntax.Token.ValueText;
        }

        identityPropertiesSymbol.Add((PropertySymbol: propertySymbol, ColumnName: columnName));
    }
}
