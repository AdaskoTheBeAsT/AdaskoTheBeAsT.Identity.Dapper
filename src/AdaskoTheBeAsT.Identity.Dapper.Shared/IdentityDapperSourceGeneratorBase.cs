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
        var dbSchemaProvider = context.AnalyzerConfigOptionsProvider.Select((
            provider,
            _) => SelectOptions(provider));

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

    internal static bool IsApplicationBase(INamedTypeSymbol possibleBase, INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, possibleBase))
            {
                return true;
            }
        }

        return false;
    }

    protected static IdentityDapperOptions SelectOptions(
                AnalyzerConfigOptionsProvider provider)
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

    protected virtual bool IsSupportedPropertyType(ITypeSymbol type) => true;

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
                => node is ClassDeclarationSyntax { BaseList: { } };

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context) =>
                context.Node as ClassDeclarationSyntax;

    private static List<INamedTypeSymbol> GetCandidates(
                Compilation compilation, IEnumerable<ClassDeclarationSyntax> declarations, CancellationToken token)
    {
        var candidates = new List<INamedTypeSymbol>();
        foreach (var declaration in declarations)
        {
            token.ThrowIfCancellationRequested();
            var model = compilation.GetSemanticModel(declaration.SyntaxTree);
            if (model.GetDeclaredSymbol(declaration, token) is INamedTypeSymbol candidate &&
                !candidate.IsAbstract && candidate.TypeParameters.Length == 0 &&
                FindIdentityBase(candidate) != null &&
                !candidates.Exists(t => SymbolEqualityComparer.Default.Equals(t, candidate)))
            {
                candidates.Add(candidate);
            }
        }

        return candidates;
    }

    private static string GetKeyTypeName(ITypeSymbol key) => key.SpecialType switch
    {
        SpecialType.System_String => "string",
        SpecialType.System_Int32 => "int",
        SpecialType.System_Int64 => "long",
        _ when string.Equals(key.ToDisplayString(), "System.Guid", StringComparison.Ordinal) => "Guid",
        _ => string.Empty,
    };

    private static bool ValidateKeyType(
                SourceProductionContext context, string localKeyType, string keyTypeName, SyntaxNode declaration)
    {
        if (string.IsNullOrEmpty(localKeyType))
        {
            ReportModelError(context, "ATBID101", "Unsupported Identity key type", declaration);
            return false;
        }

        if (!string.IsNullOrEmpty(keyTypeName) &&
            !keyTypeName.Equals(localKeyType, StringComparison.OrdinalIgnoreCase))
        {
            ReportModelError(context, "ATBID102", "Identity entities must use the same key type", declaration);
            return false;
        }

        return true;
    }

    private static bool IsCompatibleOverride(ISymbol member, IPropertySymbol identityProperty) =>
                member is IPropertySymbol property && Overrides(property, identityProperty) &&
                property.GetMethod?.DeclaredAccessibility == Accessibility.Public &&
                property.SetMethod?.DeclaredAccessibility == Accessibility.Public;

    private static bool IsPublicInstanceProperty(IPropertySymbol property) =>
                !property.IsStatic && property.DeclaredAccessibility == Accessibility.Public &&
                property.GetMethod?.DeclaredAccessibility == Accessibility.Public &&
                property.SetMethod?.DeclaredAccessibility == Accessibility.Public;

    private static bool Overrides(IPropertySymbol property, IPropertySymbol identityProperty)
    {
        for (var current = property.OverriddenProperty; current != null; current = current.OverriddenProperty)
        {
            if (SymbolEqualityComparer.Default.Equals(current, identityProperty))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRequiredIdentityProperty(string entity, string property, IdentityDapperOptions options) =>
                !(options.SkipNormalized && property is "NormalizedUserName" or "NormalizedEmail" or "NormalizedName") &&
                !(string.Equals(property, "Id", StringComparison.Ordinal) && entity is "IdentityUserClaim" or "IdentityRoleClaim");

    private static INamedTypeSymbol? FindIdentityBase(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (string.Equals(current.ContainingNamespace.ToDisplayString(), "Microsoft.AspNetCore.Identity", StringComparison.Ordinal) &&
                current.IsGenericType && current.TypeArguments.Length == 1 &&
                current.Name is "IdentityUser" or "IdentityRole" or "IdentityUserClaim" or
                    "IdentityRoleClaim" or "IdentityUserLogin" or "IdentityUserRole" or "IdentityUserToken")
            {
                return current;
            }
        }

        return null;
    }

    private static AttributeData? FindPropertyAttribute(IPropertySymbol property, string name)
    {
        for (var current = property; current != null; current = current.OverriddenProperty)
        {
            var attribute = current.GetAttributes().FirstOrDefault(a => string.Equals(a.AttributeClass?.ToDisplayString(), name, StringComparison.Ordinal));
            if (attribute != null)
            {
                return attribute;
            }
        }

        return null;
    }

    private static void ReportModelError(SourceProductionContext context, string id, string message, SyntaxNode node) =>
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(id, message, message, "Code generation", DiagnosticSeverity.Error, isEnabledByDefault: true),
                    node.GetLocation()));

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

        var candidates = GetCandidates(compilation, distinctClassDeclarations, token);
        var selected = candidates.Where(t => !candidates.Exists(other => IsApplicationBase(t, other))).ToList();
        var duplicateGroup = selected.GroupBy(t => FindIdentityBase(t)!.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Skip(1).Any());
        if (duplicateGroup != null)
        {
            ReportModelError(
context,
"ATBID103",
"Multiple application entities for the same Identity type",
duplicateGroup.Last().DeclaringSyntaxReferences[0].GetSyntax(token));
            return (string.Empty, identityPropertiesSymbol, identityTypes);
        }

        foreach (var type in selected)
        {
            token.ThrowIfCancellationRequested();
            var classDeclarationSyntax = type.DeclaringSyntaxReferences[0].GetSyntax(token);
            var identityClass = FindIdentityBase(type)!;
            var localKeyType = GetKeyTypeName(identityClass.TypeArguments[0]);
            if (!ValidateKeyType(context, localKeyType, keyTypeName, classDeclarationSyntax) ||
                !CollectProperties(context, type, identityClass, options, identityPropertiesSymbol, token))
            {
                return (string.Empty, new List<(IPropertySymbol, string)>(), new List<INamedTypeSymbol>());
            }

            keyTypeName = localKeyType;
            identityTypes.Add(type);
        }

        return (keyTypeName, identityPropertiesSymbol, identityTypes);
    }

    private bool CollectProperties(
            SourceProductionContext context,
            INamedTypeSymbol type,
            INamedTypeSymbol identityClass,
            IdentityDapperOptions options,
            IList<(IPropertySymbol PropertySymbol, string ColumnName)> properties,
            CancellationToken token)
    {
        var declaration = type.DeclaringSyntaxReferences[0].GetSyntax(token);
        var identityProperties = identityClass.GetMembers().OfType<IPropertySymbol>()
            .ToDictionary(p => p.Name, StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var current = type; current != null &&
!string.Equals(current.ContainingNamespace.ToDisplayString(), "Microsoft.AspNetCore.Identity", StringComparison.Ordinal); current = current.BaseType)
        {
            foreach (var member in current.GetMembers().Where(m => !m.IsImplicitlyDeclared))
            {
                // Resolve shadowing before filtering accessors or attributes. Otherwise an
                // excluded derived member can accidentally resurrect a mapped base property.
                if (!names.Add(member.Name))
                {
                    continue;
                }

                if (!CollectProperty(context, member, identityProperties, identityClass.Name, declaration, options, properties))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool CollectProperty(
            SourceProductionContext context,
            ISymbol member,
            Dictionary<string, IPropertySymbol> identityProperties,
            string entity,
            SyntaxNode declaration,
            IdentityDapperOptions options,
            IList<(IPropertySymbol PropertySymbol, string ColumnName)> properties)
    {
        var location = member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken) ?? declaration;
        var isIdentityProperty = identityProperties.TryGetValue(member.Name, out var identityProperty);
        if (isIdentityProperty && !IsCompatibleOverride(member, identityProperty))
        {
            ReportModelError(
context,
"ATBID106",
$"Identity property '{member.Name}' requires a compatible public override with public get and set accessors; hiding is not supported",
location);
            return false;
        }

        if (member is not IPropertySymbol property || property.IsIndexer)
        {
            return true;
        }

        var ignored = FindPropertyAttribute(property, "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute") != null;
        if (ignored && isIdentityProperty && IsRequiredIdentityProperty(entity, property.Name, options))
        {
            ReportModelError(
context,
"ATBID105",
$"Required Identity property '{property.Name}' cannot be excluded with [NotMapped]",
location);
            return false;
        }

        if (!IsPublicInstanceProperty(property))
        {
            return true;
        }

        if (!ignored && !IsSupportedPropertyType(property.Type))
        {
            ReportModelError(context, "ATBID104", $"Unsupported mapped property type: {property.Type}", location);
            return false;
        }

        var column = FindPropertyAttribute(property, "System.ComponentModel.DataAnnotations.Schema.ColumnAttribute");
        var columnName = column?.ConstructorArguments.FirstOrDefault().Value as string ?? property.Name;
        properties.Add((property, ignored ? string.Empty : columnName));
        return true;
    }
}
