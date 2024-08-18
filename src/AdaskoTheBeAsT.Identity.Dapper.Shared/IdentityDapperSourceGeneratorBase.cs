using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        var dbSchemaProvider = context.AnalyzerConfigOptionsProvider.Select(SelectOptions);

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
            context.CompilationProvider.Combine(dbSchemaProvider).Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(
            compilationAndClasses,
            (
                    spc,
                    source) =>
                Execute(spc, source.Left.Left, source.Left.Right, source.Right));
    }

    protected IdentityDapperOptions SelectOptions(
        AnalyzerConfigOptionsProvider provider,
        CancellationToken token)
    {
        var dbSchema = "dbo";
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AdaskoTheBeAsTIdentityDapper_DbSchema",
                out var schemaProperty))
        {
            dbSchema = schemaProperty;
        }

        var skipNormalized = false;
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AdaskoTheBeAsTIdentityDapper_SkipNormalized",
                out var strValue) && bool.TryParse(strValue, out var result))
        {
            skipNormalized = result;
        }

        var storeBooleanAs = string.Empty;
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AdaskoTheBeAsTIdentityDapper_StoreBooleanAs",
                out var storeBooleanAsProperty))
        {
            storeBooleanAs = storeBooleanAsProperty;
        }

        return new IdentityDapperOptions(dbSchema, skipNormalized, storeBooleanAs);
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
        SourceProductionContext context,
        Compilation compilation,
        IdentityDapperOptions options,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations)
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
            context,
            compilation,
            distinctClassDeclarations,
            context.CancellationToken);

        // If there were errors in the EnumDeclarationSyntax, we won't create an
        // EnumToGenerate for it, so make sure we have something to generate
        if (classesToGenerate.Items.Count > 0)
        {
            // generate the source code and add it to the output
            _sourceGeneratorHelper.GenerateCode(context, compilation, options, classesToGenerate);
        }
    }

    private (string KeyTypeName, IList<(IPropertySymbol PropertySymbol, string ColumnName)> Items) GetTypesToGenerate(
        SourceProductionContext context,
        Compilation compilation,
        IEnumerable<ClassDeclarationSyntax>? distinctClassDeclarations,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var identityPropertiesSymbol = new List<(IPropertySymbol PropertySymbol, string ColumnName)>();
        if (distinctClassDeclarations == null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "ATBID100",
                        "No identity classes defined",
                        "No identity classes defined - please provide all classes which inherits from Identity...<type of key>",
                        "Code generation",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location: null,
                    Array.Empty<object>()));
            return (string.Empty, identityPropertiesSymbol);
        }

        var keyTypeName = string.Empty;

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
            if (!string.IsNullOrEmpty(keyTypeName) &&
                !keyTypeName.Equals(localKeyType, StringComparison.OrdinalIgnoreCase))
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
