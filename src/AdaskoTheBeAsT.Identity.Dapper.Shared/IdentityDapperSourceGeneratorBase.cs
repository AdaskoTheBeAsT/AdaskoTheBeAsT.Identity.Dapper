using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityDapperSourceGeneratorBase
{
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
        var dbSchema = string.Empty;
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
        => node is ClassDeclarationSyntax { BaseList: { } };

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context) =>
        context.Node as ClassDeclarationSyntax;

    protected virtual bool IsSupportedPropertyType(ITypeSymbol type) => true;

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
            options,
            context.CancellationToken);

        // If there were errors in the EnumDeclarationSyntax, we won't create an
        // EnumToGenerate for it, so make sure we have something to generate
        if (classesToGenerate.Types.Count > 0)
        {
            // generate the source code and add it to the output
            _sourceGeneratorHelper.GenerateCode(context, compilation, options, classesToGenerate);
        }
    }

    private (string KeyTypeName, IList<(IPropertySymbol PropertySymbol, string ColumnName)> Items, IList<INamedTypeSymbol> Types) GetTypesToGenerate(
        SourceProductionContext context,
        Compilation compilation,
        IEnumerable<ClassDeclarationSyntax>? distinctClassDeclarations,
        IdentityDapperOptions options,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var identityPropertiesSymbol = new List<(IPropertySymbol PropertySymbol, string ColumnName)>();
        var identityTypes = new List<INamedTypeSymbol>();
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
            return (string.Empty, identityPropertiesSymbol, identityTypes);
        }

        var keyTypeName = string.Empty;

        var candidates = new List<INamedTypeSymbol>();
        foreach (var declaration in distinctClassDeclarations)
        {
            var model = compilation.GetSemanticModel(declaration.SyntaxTree);
            if (model.GetDeclaredSymbol(declaration) is INamedTypeSymbol candidate &&
                !candidate.IsAbstract && candidate.TypeParameters.Length == 0 &&
                FindIdentityBase(candidate) != null &&
                !candidates.Any(t => SymbolEqualityComparer.Default.Equals(t, candidate)))
            {
                candidates.Add(candidate);
            }
        }

        var selected = candidates.Where(t => !candidates.Any(other => IsApplicationBase(t, other))).ToList();
        foreach (var group in selected.GroupBy(t => FindIdentityBase(t)!.Name))
        {
            if (group.Count() > 1)
            {
                ReportModelError(context, "ATBID103", "Multiple application entities for the same Identity type",
                    group.Last().DeclaringSyntaxReferences[0].GetSyntax(token));
                return (string.Empty, identityPropertiesSymbol, identityTypes);
            }
        }

        foreach (var type in selected)
        {
            token.ThrowIfCancellationRequested();
            var classDeclarationSyntax = type.DeclaringSyntaxReferences[0].GetSyntax(token);
            var identityClass = FindIdentityBase(type)!;
            var key = identityClass.TypeArguments[0];
            var localKeyType = key.SpecialType switch
            {
                SpecialType.System_String => "string",
                SpecialType.System_Int32 => "int",
                SpecialType.System_Int64 => "long",
                _ when key.ToDisplayString() == "System.Guid" => "Guid",
                _ => string.Empty,
            };
            if (string.IsNullOrEmpty(localKeyType))
            {
                ReportModelError(context, "ATBID101", "Unsupported Identity key type", classDeclarationSyntax);
                return (string.Empty, new List<(IPropertySymbol, string)>(), new List<INamedTypeSymbol>());
            }

            if (!string.IsNullOrEmpty(keyTypeName) &&
                !keyTypeName.Equals(localKeyType, StringComparison.OrdinalIgnoreCase))
            {
                ReportModelError(context, "ATBID102", "Identity entities must use the same key type", classDeclarationSyntax);
                return (string.Empty, new List<(IPropertySymbol, string)>(), new List<INamedTypeSymbol>());
            }

            keyTypeName = localKeyType;
            identityTypes.Add(type);
            var identityProperties = identityClass.GetMembers().OfType<IPropertySymbol>()
                .ToDictionary(p => p.Name, StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var current = type; current != null &&
                current.ContainingNamespace.ToDisplayString() != "Microsoft.AspNetCore.Identity"; current = current.BaseType)
            {
                foreach (var member in current.GetMembers().Where(m => !m.IsImplicitlyDeclared))
                {
                    // Resolve shadowing before filtering accessors or attributes. Otherwise an
                    // excluded derived member can accidentally resurrect a mapped base property.
                    if (!names.Add(member.Name)) continue;
                    var location = member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(token) ?? classDeclarationSyntax;
                    var isIdentityProperty = identityProperties.TryGetValue(member.Name, out var identityProperty);
                    if (isIdentityProperty &&
                        (member is not IPropertySymbol overridingProperty || !Overrides(overridingProperty, identityProperty!) ||
                         overridingProperty.GetMethod?.DeclaredAccessibility != Accessibility.Public ||
                         overridingProperty.SetMethod?.DeclaredAccessibility != Accessibility.Public))
                    {
                        ReportModelError(context, "ATBID106",
                            $"Identity property '{member.Name}' requires a compatible public override with public get and set accessors; hiding is not supported", location);
                        return (string.Empty, new List<(IPropertySymbol, string)>(), new List<INamedTypeSymbol>());
                    }

                    if (member is not IPropertySymbol property || property.IsIndexer) continue;
                    var ignored = FindPropertyAttribute(property, "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute") != null;
                    if (ignored && isIdentityProperty && IsRequiredIdentityProperty(identityClass.Name, property.Name, options))
                    {
                        ReportModelError(context, "ATBID105",
                            $"Required Identity property '{property.Name}' cannot be excluded with [NotMapped]", location);
                        return (string.Empty, new List<(IPropertySymbol, string)>(), new List<INamedTypeSymbol>());
                    }

                    if (property.IsStatic || property.DeclaredAccessibility != Accessibility.Public ||
                        property.GetMethod?.DeclaredAccessibility != Accessibility.Public ||
                        property.SetMethod?.DeclaredAccessibility != Accessibility.Public)
                    {
                        continue;
                    }

                    var column = FindPropertyAttribute(property, "System.ComponentModel.DataAnnotations.Schema.ColumnAttribute");
                    var columnName = column?.ConstructorArguments.FirstOrDefault().Value as string ?? property.Name;
                    if (!ignored && !IsSupportedPropertyType(property.Type))
                    {
                        ReportModelError(context, "ATBID104", $"Unsupported mapped property type: {property.Type}",
                            property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(token) ?? classDeclarationSyntax);
                        return (string.Empty, new List<(IPropertySymbol, string)>(), new List<INamedTypeSymbol>());
                    }

                    identityPropertiesSymbol.Add((property, ignored ? string.Empty : columnName));
                }
            }
        }

        return (keyTypeName, identityPropertiesSymbol, identityTypes);
    }

    private static bool Overrides(IPropertySymbol property, IPropertySymbol identityProperty)
    {
        for (var current = property.OverriddenProperty; current != null; current = current.OverriddenProperty)
        {
            if (SymbolEqualityComparer.Default.Equals(current, identityProperty)) return true;
        }

        return false;
    }

    private static bool IsRequiredIdentityProperty(string entity, string property, IdentityDapperOptions options) =>
        !(options.SkipNormalized && property is "NormalizedUserName" or "NormalizedEmail" or "NormalizedName") &&
        !(property == "Id" && entity is "IdentityUserClaim" or "IdentityRoleClaim");

    private static INamedTypeSymbol? FindIdentityBase(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Identity" &&
                current.IsGenericType && current.TypeArguments.Length == 1 &&
                current.Name is "IdentityUser" or "IdentityRole" or "IdentityUserClaim" or
                    "IdentityRoleClaim" or "IdentityUserLogin" or "IdentityUserRole" or "IdentityUserToken")
            {
                return current;
            }
        }

        return null;
    }

    internal static bool IsApplicationBase(INamedTypeSymbol possibleBase, INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, possibleBase)) return true;
        }

        return false;
    }

    private static AttributeData? FindPropertyAttribute(IPropertySymbol property, string name)
    {
        for (var current = property; current != null; current = current.OverriddenProperty)
        {
            var attribute = current.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == name);
            if (attribute != null) return attribute;
        }

        return null;
    }

    private static void ReportModelError(SourceProductionContext context, string id, string message, SyntaxNode node) =>
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(id, message, message, "Code generation", DiagnosticSeverity.Error, true),
            node.GetLocation()));
}
