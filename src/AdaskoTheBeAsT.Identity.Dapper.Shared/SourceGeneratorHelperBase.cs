using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.Attributes;
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

    public void GenerateCode(
        SourceProductionContext context,
        Compilation compilation,
        IdentityDapperOptions options,
        (string KeyTypeName, IList<(IPropertySymbol PropertySymbol, string ColumnName)> Items) generationInfo)
    {
        var grouped = generationInfo
            .Items
            .GroupBy<(IPropertySymbol, string), INamedTypeSymbol>(
                p => p.Item1.ContainingType,
                SymbolEqualityComparer.Default);

        var set = new HashSet<string>(_entityNames, StringComparer.OrdinalIgnoreCase);
        var namespaceName = "EmptyNamespace";

        var schemaPart = GenerateSchemaPart(options.Schema);
        var attributeTypeSymbol = compilation.GetTypeByMetadataName("AdaskoTheBeAsT.Identity.Dapper.Attributes.InsertOwnIdAttribute");

        foreach (var group in grouped)
        {
            var baseTypeName = group.Key.BaseType?.Name ?? string.Empty;
            namespaceName = group.Key.ContainingNamespace.ToDisplayString();
            var insertOwnId = group.Key.GetAttributes()
                .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeTypeSymbol));

            var config = new IdentityDapperConfiguration(
                baseTypeName,
                generationInfo.KeyTypeName,
                namespaceName,
                schemaPart,
                options.SkipNormalized,
                insertOwnId);
            ProcessClass(context, config, group.ToList());
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
                false);
            ProcessClass(context, config, new List<(IPropertySymbol PropertySymbol, string ColumnName)>());
        }

        ProcessApplicationStores(context, generationInfo.KeyTypeName, namespaceName);
    }

    protected abstract string GenerateSchemaPart(string dbSchema);

    private void ProcessClass(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> list)
    {
        var propertyColumnPairs = Extract(list);
        switch (config.BaseTypeName)
        {
            case nameof(IdentityRole):
                ProcessIdentityRole(context, config, propertyColumnPairs);
                break;
            case nameof(IdentityRoleClaim<int>):
                ProcessIdentityRoleClaim(context, config, propertyColumnPairs);
                break;
            case nameof(IdentityUser):
                ProcessIdentityUser(context, config, propertyColumnPairs);
                ProcessIdentityUserRoleClaim(context, config);
                break;
            case nameof(IdentityUserClaim<int>):
                ProcessIdentityUserClaim(context, config, propertyColumnPairs);
                break;
            case nameof(IdentityUserLogin<int>):
                ProcessIdentityUserLogin(context, config, propertyColumnPairs);
                break;
            case nameof(IdentityUserRole<int>):
                ProcessIdentityUserRole(context, config, propertyColumnPairs);
                break;
            case nameof(IdentityUserToken<int>):
                ProcessIdentityUserToken(context, config, propertyColumnPairs);
                break;

            // ReSharper disable once RedundantEmptySwitchSection
            default:
                break;
        }
    }

    private void ProcessIdentityRole(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnPair> propertyColumnPairs)
    {
        var content = _identityRoleClassGenerator.Generate(config, propertyColumnPairs);

        context.AddSource("IdentityRoleSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityRoleClaim(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnPair> propertyColumnPairs)
    {
        var content = _identityRoleClaimClassGenerator.Generate(config, propertyColumnPairs);

        context.AddSource("IdentityRoleClaimSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUser(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnPair> propertyColumnPairs)
    {
        var content = _identityUserClassGenerator.Generate(config, propertyColumnPairs);

        context.AddSource("IdentityUserSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUserRoleClaim(
        SourceProductionContext context,
        IdentityDapperConfiguration config)
    {
        var content = _identityUserRoleClaimClassGenerator.Generate(config);

        context.AddSource("IdentityUserRoleClaimSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUserClaim(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnPair> propertyColumnPairs)
    {
        var content = _identityUserClaimClassGenerator.Generate(config, propertyColumnPairs);

        context.AddSource("IdentityUserClaimSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUserLogin(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnPair> propertyColumnPairs)
    {
        var content = _identityUserLoginClassGenerator.Generate(config, propertyColumnPairs);

        context.AddSource("IdentityUserLoginSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUserRole(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnPair> propertyColumnPairs)
    {
        var content = _identityUserRoleClassGenerator.Generate(config, propertyColumnPairs);

        context.AddSource("IdentityUserRoleSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUserToken(
        SourceProductionContext context,
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnPair> propertyColumnPairs)
    {
        var content = _identityUserTokenClassGenerator.Generate(config, propertyColumnPairs);

        context.AddSource("IdentityUserTokenSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private IEnumerable<PropertyColumnPair> Extract(
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> list) =>
        list.Select(i => new PropertyColumnPair(i.PropertySymbol.Name, i.ColumnName));

    private void ProcessApplicationStores(
        SourceProductionContext context,
        string keyTypeName,
        string namespaceName)
    {
        ProcessApplicationUserOnlyStore(context, keyTypeName, namespaceName);
        ProcessApplicationUserStore(context, keyTypeName, namespaceName);
        ProcessApplicationRoleStore(context, keyTypeName, namespaceName);
    }

    private void ProcessApplicationUserOnlyStore(
        SourceProductionContext context,
        string keyTypeName,
        string namespaceName)
    {
        var content = _applicationUserOnlyStoreGenerator.Generate(keyTypeName, namespaceName);

        context.AddSource("ApplicationUserOnlyStore.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessApplicationUserStore(
        SourceProductionContext context,
        string keyTypeName,
        string namespaceName)
    {
        var content = _applicationUserStoreGenerator.Generate(keyTypeName, namespaceName);

        context.AddSource("ApplicationUserStore.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessApplicationRoleStore(
        SourceProductionContext context,
        string keyTypeName,
        string namespaceName)
    {
        var content = _applicationRoleStoreGenerator.Generate(keyTypeName, namespaceName);

        context.AddSource("ApplicationRoleStore.g.cs", SourceText.From(content, Encoding.UTF8));
    }
}
