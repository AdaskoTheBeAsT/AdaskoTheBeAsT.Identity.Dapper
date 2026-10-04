using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class SourceGeneratorHelperBase
    : ISourceGeneratorHelper
{
    private readonly IList<string> _entityNames = new List<string>
    {
        nameof(IdentityRole),
        nameof(IdentityRoleClaim<int>),
        nameof(IdentityUser),
        nameof(IdentityUserClaim<int>),
        nameof(IdentityUserLogin<int>),
        nameof(IdentityUserRole<int>),
        nameof(IdentityUserToken<int>),
    };

    private readonly IIdentityRoleClassGenerator _identityRoleClassGenerator;
    private readonly IIdentityRoleClaimClassGenerator _identityRoleClaimClassGenerator;
    private readonly IIdentityUserClassGenerator _identityUserClassGenerator;
    private readonly IIdentityUserClaimClassGenerator _identityUserClaimClassGenerator;
    private readonly IIdentityUserLoginClassGenerator _identityUserLoginClassGenerator;
    private readonly IIdentityUserRoleClassGenerator _identityUserRoleClassGenerator;
    private readonly IIdentityUserTokenClassGenerator _identityUserTokenClassGenerator;
    private readonly IIdentityUserRoleClaimClassGenerator _identityUserRoleClaimClassGenerator;
    private readonly IApplicationUserOnlyStoreGenerator _applicationUserOnlyStoreGenerator;
    private readonly IApplicationUserStoreGenerator _applicationUserStoreGenerator;
    private readonly IApplicationRoleStoreGenerator _applicationRoleStoreGenerator;

    protected SourceGeneratorHelperBase(
        IIdentityRoleClassGenerator identityRoleClassGenerator,
        IIdentityRoleClaimClassGenerator identityRoleClaimClassGenerator,
        IIdentityUserClassGenerator identityUserClassGenerator,
        IIdentityUserClaimClassGenerator identityUserClaimClassGenerator,
        IIdentityUserLoginClassGenerator identityUserLoginClassGenerator,
        IIdentityUserRoleClassGenerator identityUserRoleClassGenerator,
        IIdentityUserTokenClassGenerator identityUserTokenClassGenerator,
        IIdentityUserRoleClaimClassGenerator identityUserRoleClaimClassGenerator,
        IApplicationUserOnlyStoreGenerator applicationUserOnlyStoreGenerator,
        IApplicationUserStoreGenerator applicationUserStoreGenerator,
        IApplicationRoleStoreGenerator applicationRoleStoreGenerator)
    {
        _identityRoleClassGenerator = identityRoleClassGenerator;
        _identityRoleClaimClassGenerator = identityRoleClaimClassGenerator;
        _identityUserClassGenerator = identityUserClassGenerator;
        _identityUserClaimClassGenerator = identityUserClaimClassGenerator;
        _identityUserLoginClassGenerator = identityUserLoginClassGenerator;
        _identityUserRoleClassGenerator = identityUserRoleClassGenerator;
        _identityUserTokenClassGenerator = identityUserTokenClassGenerator;
        _identityUserRoleClaimClassGenerator = identityUserRoleClaimClassGenerator;
        _applicationUserOnlyStoreGenerator = applicationUserOnlyStoreGenerator;
        _applicationUserStoreGenerator = applicationUserStoreGenerator;
        _applicationRoleStoreGenerator = applicationRoleStoreGenerator;
    }

    protected virtual DatabaseProvider Provider => DatabaseProvider.SqlServer;

#pragma warning disable MA0051 // Method is too long
    public void GenerateCode(
        SourceProductionContext context,
        Compilation compilation,
        IdentityDapperOptions options,
        (string KeyTypeName, IList<(IPropertySymbol PropertySymbol, string ColumnName)> Items, IList<INamedTypeSymbol> Types) generationInfo)
    {
        var set = new HashSet<string>(_entityNames, StringComparer.OrdinalIgnoreCase);
        var namespaceName = "EmptyNamespace";

        var schemaPart = GenerateSchemaPart(options.Schema);
        var attributeTypeSymbol = compilation.GetTypeByMetadataName("AdaskoTheBeAsT.Identity.Dapper.Attributes.InsertOwnIdAttribute");
        var mappings = new Dictionary<string, IDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var type in generationInfo.Types)
        {
            mappings[GetIdentityBaseName(type)] = GetPropertiesForType(generationInfo.Items, type)
                .ToDictionary(p => p.PropertySymbol.Name, p => p.ColumnName, StringComparer.Ordinal);
        }

        var typePropertiesDict = new Dictionary<string, IList<PropertyColumnTypeTriple>>(StringComparer.OrdinalIgnoreCase);
        var userInsertOwnId = false;
        var roleInsertOwnId = false;
        foreach (var type in generationInfo.Types)
        {
            var baseTypeName = GetIdentityBaseName(type);
            namespaceName = type.ContainingNamespace.ToDisplayString();
            var insertOwnId = HasAttribute(type, attributeTypeSymbol);
            if (insertOwnId)
            {
                if (string.Equals(baseTypeName, nameof(IdentityUser), StringComparison.Ordinal))
                {
                    userInsertOwnId = true;
                }
                else if (string.Equals(baseTypeName, nameof(IdentityRole), StringComparison.Ordinal))
                {
                    roleInsertOwnId = true;
                }
            }

            var config = new IdentityDapperConfiguration(
                baseTypeName,
                generationInfo.KeyTypeName,
                namespaceName,
                schemaPart,
                options.SkipNormalized,
                insertOwnId)
            { Provider = Provider, ColumnMappings = mappings };
            var allProperties = ProcessClass(context, config, GetPropertiesForType(generationInfo.Items, type).ToList());
            typePropertiesDict[baseTypeName] = allProperties;
            set.Remove(baseTypeName);
        }

        foreach (var baseTypeName in set)
        {
            var config = new IdentityDapperConfiguration(
                baseTypeName,
                generationInfo.KeyTypeName,
                namespaceName,
                schemaPart,
                options.SkipNormalized,
                false)
            { Provider = Provider, ColumnMappings = mappings };
            var allProperties = ProcessClass(
                context,
                config,
                new List<(IPropertySymbol PropertySymbol, string ColumnName)>());
            typePropertiesDict[baseTypeName] = allProperties;
        }

        ProcessApplicationStores(
            context,
            typePropertiesDict,
            options,
            generationInfo.KeyTypeName,
            namespaceName,
            userInsertOwnId,
            roleInsertOwnId);

        GenerateAdditionalFiles(context, options);
    }
#pragma warning restore MA0051

    protected abstract string GenerateSchemaPart(string dbSchema);

    protected abstract void GenerateAdditionalFiles(
        SourceProductionContext context,
        IdentityDapperOptions options);

    private static string GetIdentityBaseName(INamedTypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType != null && !string.Equals(baseType.ContainingNamespace.ToDisplayString(), "Microsoft.AspNetCore.Identity", StringComparison.Ordinal))
        {
            baseType = baseType.BaseType;
        }

        return baseType?.Name ?? string.Empty;
    }

    private static IEnumerable<(IPropertySymbol PropertySymbol, string ColumnName)> GetPropertiesForType(
        IEnumerable<(IPropertySymbol PropertySymbol, string ColumnName)> properties, INamedTypeSymbol type) =>
        properties.Where(p => SymbolEqualityComparer.Default.Equals(p.PropertySymbol.ContainingType, type) ||
            IdentityDapperSourceGeneratorBase.IsApplicationBase(p.PropertySymbol.ContainingType, type));

    private static bool HasAttribute(INamedTypeSymbol type, INamedTypeSymbol? attributeType)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<PropertyColumnTypeTriple> Extract(
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> list) =>
        list.Select(
            i => new PropertyColumnTypeTriple(
                i.PropertySymbol.Name,
                i.PropertySymbol.Type.ToDisplayString(),
                i.ColumnName));

    private IList<PropertyColumnTypeTriple> ProcessClass(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> list)
    {
        var propertyColumnTypeTriples = Extract(list);
        switch (config.BaseTypeName)
        {
            case nameof(IdentityRole):
                return ProcessIdentityRole(context, config, propertyColumnTypeTriples);
            case nameof(IdentityRoleClaim<int>):
                return ProcessIdentityRoleClaim(context, config, propertyColumnTypeTriples);
            case nameof(IdentityUser):
                ProcessIdentityUserRoleClaim(context, config);
                return ProcessIdentityUser(context, config, propertyColumnTypeTriples);
            case nameof(IdentityUserClaim<int>):
                return ProcessIdentityUserClaim(context, config, propertyColumnTypeTriples);
            case nameof(IdentityUserLogin<int>):
                return ProcessIdentityUserLogin(context, config, propertyColumnTypeTriples);
            case nameof(IdentityUserRole<int>):
                return ProcessIdentityUserRole(context, config, propertyColumnTypeTriples);
            case nameof(IdentityUserToken<int>):
                return ProcessIdentityUserToken(context, config, propertyColumnTypeTriples);

            // ReSharper disable once RedundantEmptySwitchSection
            default:
                return new List<PropertyColumnTypeTriple>();
        }
    }

    private IList<PropertyColumnTypeTriple> ProcessIdentityRole(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var propertyColumnTypeTriples = _identityRoleClassGenerator.GetAllProperties(
            customs,
            config.InsertOwnId);
        var content = _identityRoleClassGenerator.Generate(config, propertyColumnTypeTriples);

        context.AddSource("IdentityRoleSql.g.cs", SourceText.From(content, Encoding.UTF8));

        return propertyColumnTypeTriples;
    }

    private IList<PropertyColumnTypeTriple> ProcessIdentityRoleClaim(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var propertyColumnTypeTriples = _identityRoleClaimClassGenerator.GetAllProperties(
            customs,
            config.InsertOwnId);
        var content = _identityRoleClaimClassGenerator.Generate(config, propertyColumnTypeTriples);

        context.AddSource("IdentityRoleClaimSql.g.cs", SourceText.From(content, Encoding.UTF8));
        return propertyColumnTypeTriples;
    }

    private IList<PropertyColumnTypeTriple> ProcessIdentityUser(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var propertyColumnTypeTriples = _identityUserClassGenerator.GetAllProperties(
            customs,
            config.InsertOwnId);
        var content = _identityUserClassGenerator.Generate(config, propertyColumnTypeTriples);

        context.AddSource("IdentityUserSql.g.cs", SourceText.From(content, Encoding.UTF8));
        return propertyColumnTypeTriples;
    }

    private void ProcessIdentityUserRoleClaim(
        SourceProductionContext context,
        IdentityDapperConfiguration config)
    {
        var content = _identityUserRoleClaimClassGenerator.Generate(config);

        context.AddSource("IdentityUserRoleClaimSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private IList<PropertyColumnTypeTriple> ProcessIdentityUserClaim(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var propertyColumnTypeTriples = _identityUserClaimClassGenerator.GetAllProperties(
            customs,
            config.InsertOwnId);
        var content = _identityUserClaimClassGenerator.Generate(config, propertyColumnTypeTriples);

        context.AddSource("IdentityUserClaimSql.g.cs", SourceText.From(content, Encoding.UTF8));
        return propertyColumnTypeTriples;
    }

    private IList<PropertyColumnTypeTriple> ProcessIdentityUserLogin(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var propertyColumnTypeTriples = _identityUserLoginClassGenerator.GetAllProperties(
            customs,
            config.InsertOwnId);
        var content = _identityUserLoginClassGenerator.Generate(config, propertyColumnTypeTriples);

        context.AddSource("IdentityUserLoginSql.g.cs", SourceText.From(content, Encoding.UTF8));
        return propertyColumnTypeTriples;
    }

    private IList<PropertyColumnTypeTriple> ProcessIdentityUserRole(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var propertyColumnTypeTriples = _identityUserRoleClassGenerator.GetAllProperties(
            customs,
            config.InsertOwnId);
        var content = _identityUserRoleClassGenerator.Generate(config, propertyColumnTypeTriples);

        context.AddSource("IdentityUserRoleSql.g.cs", SourceText.From(content, Encoding.UTF8));
        return propertyColumnTypeTriples;
    }

    private IList<PropertyColumnTypeTriple> ProcessIdentityUserToken(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnTypeTriple> customs)
    {
        var propertyColumnTypeTriples = _identityUserTokenClassGenerator.GetAllProperties(
            customs,
            config.InsertOwnId);
        var content = _identityUserTokenClassGenerator.Generate(config, propertyColumnTypeTriples);

        context.AddSource("IdentityUserTokenSql.g.cs", SourceText.From(content, Encoding.UTF8));
        return propertyColumnTypeTriples;
    }

    private void ProcessApplicationStores(
        SourceProductionContext context,
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        string keyTypeName,
        string namespaceName,
        bool userInsertOwnId,
        bool roleInsertOwnId)
    {
        ProcessApplicationUserOnlyStore(context, typePropertiesDict, options, keyTypeName, namespaceName, userInsertOwnId);
        ProcessApplicationUserStore(context, typePropertiesDict, options, keyTypeName, namespaceName, userInsertOwnId);
        ProcessApplicationRoleStore(context, typePropertiesDict, options, keyTypeName, namespaceName, roleInsertOwnId);
    }

    private void ProcessApplicationUserOnlyStore(
        SourceProductionContext context,
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        string keyTypeName,
        string namespaceName,
        bool insertOwnId)
    {
        var content = _applicationUserOnlyStoreGenerator.Generate(
            typePropertiesDict,
            options,
            keyTypeName,
            namespaceName,
            insertOwnId);

        context.AddSource("ApplicationUserOnlyStore.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessApplicationUserStore(
        SourceProductionContext context,
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        string keyTypeName,
        string namespaceName,
        bool insertOwnId)
    {
        var content = _applicationUserStoreGenerator.Generate(typePropertiesDict, options, keyTypeName, namespaceName, insertOwnId);

        context.AddSource("ApplicationUserStore.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessApplicationRoleStore(
        SourceProductionContext context,
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        string keyTypeName,
        string namespaceName,
        bool insertOwnId)
    {
        var content = _applicationRoleStoreGenerator.Generate(
            typePropertiesDict,
            options,
            keyTypeName,
            namespaceName,
            insertOwnId);

        context.AddSource("ApplicationRoleStore.g.cs", SourceText.From(content, Encoding.UTF8));
    }
}
