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

    protected SourceGeneratorHelperBase(
        IIdentityRoleClassGenerator identityRoleClassGenerator,
        IIdentityRoleClaimClassGenerator identityRoleClaimClassGenerator,
        IIdentityUserClassGenerator identityUserClassGenerator,
        IIdentityUserClaimClassGenerator identityUserClaimClassGenerator,
        IIdentityUserLoginClassGenerator identityUserLoginClassGenerator,
        IIdentityUserRoleClassGenerator identityUserRoleClassGenerator,
        IIdentityUserTokenClassGenerator identityUserTokenClassGenerator)
    {
        _identityRoleClassGenerator = identityRoleClassGenerator;
        _identityRoleClaimClassGenerator = identityRoleClaimClassGenerator;
        _identityUserClassGenerator = identityUserClassGenerator;
        _identityUserClaimClassGenerator = identityUserClaimClassGenerator;
        _identityUserLoginClassGenerator = identityUserLoginClassGenerator;
        _identityUserRoleClassGenerator = identityUserRoleClassGenerator;
        _identityUserTokenClassGenerator = identityUserTokenClassGenerator;
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

        var set = new HashSet<string>(_entityNames, StringComparer.OrdinalIgnoreCase);
        var namespaceName = "EmptyNamespace";

        foreach (var group in grouped)
        {
            var baseTypeName = group.Key.BaseType?.Name ?? string.Empty;
            namespaceName = group.Key.ContainingNamespace.ToDisplayString();
            ProcessClass(context, baseTypeName, namespaceName, group.ToList());
            set.Remove(baseTypeName);
        }

        foreach (var baseTypeName in set)
        {
            ProcessClass(context, baseTypeName, namespaceName, new List<(IPropertySymbol PropertySymbol, string ColumnName)>());
        }
    }

    private void ProcessClass(
        SourceProductionContext context,
        string baseTypeName,
        string namespaceName,
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> list)
    {
        var (propertyNames, columnNames) = Extract(list);
        switch (baseTypeName)
        {
            case nameof(IdentityRole):
                ProcessIdentityRole(context, namespaceName, propertyNames, columnNames);
                break;
            case nameof(IdentityRoleClaim<int>):
                ProcessIdentityRoleClaim(context, namespaceName, propertyNames, columnNames);
                break;
            case nameof(IdentityUser):
                ProcessIdentityUser(context, namespaceName, propertyNames, columnNames);
                break;
            case nameof(IdentityUserClaim<int>):
                ProcessIdentityUserClaim(context, namespaceName, propertyNames, columnNames);
                break;
            case nameof(IdentityUserLogin<int>):
                ProcessIdentityUserLogin(context, namespaceName, propertyNames, columnNames);
                break;
            case nameof(IdentityUserRole<int>):
                ProcessIdentityUserRole(context, namespaceName, propertyNames, columnNames);
                break;
            case nameof(IdentityUserToken<int>):
                ProcessIdentityUserToken(context, namespaceName, propertyNames, columnNames);
                break;

            // ReSharper disable once RedundantEmptySwitchSection
            default:
                break;
        }
    }

    private void ProcessIdentityRole(
        SourceProductionContext context,
        string namespaceName,
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames)
    {
        var content = _identityRoleClassGenerator.Generate(namespaceName, propertyNames, columnNames);

        context.AddSource("IdentityRoleSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityRoleClaim(
        SourceProductionContext context,
        string namespaceName,
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames)
    {
        var content = _identityRoleClaimClassGenerator.Generate(namespaceName, propertyNames, columnNames);

        context.AddSource("IdentityRoleClaimSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUser(
        SourceProductionContext context,
        string namespaceName,
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames)
    {
        var content = _identityUserClassGenerator.Generate(namespaceName, propertyNames, columnNames);

        context.AddSource("IdentityUserSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUserClaim(
        SourceProductionContext context,
        string namespaceName,
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames)
    {
        var content = _identityUserClaimClassGenerator.Generate(namespaceName, propertyNames, columnNames);

        context.AddSource("IdentityUserClaimSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUserLogin(
        SourceProductionContext context,
        string namespaceName,
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames)
    {
        var content = _identityUserLoginClassGenerator.Generate(namespaceName, propertyNames, columnNames);

        context.AddSource("IdentityUserLoginSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUserRole(
        SourceProductionContext context,
        string namespaceName,
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames)
    {
        var content = _identityUserRoleClassGenerator.Generate(namespaceName, propertyNames, columnNames);

        context.AddSource("IdentityUserRoleSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void ProcessIdentityUserToken(
        SourceProductionContext context,
        string namespaceName,
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames)
    {
        var content = _identityUserTokenClassGenerator.Generate(namespaceName, propertyNames, columnNames);

        context.AddSource("IdentityUserTokenSql.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private (IEnumerable<string> PropertyNames, IEnumerable<string> ColumnNames) Extract(
        IList<(IPropertySymbol PropertySymbol, string ColumnName)> list) =>
        (list.Select(i => i.PropertySymbol.Name),
            list.Select(i => i.ColumnName));
}
