using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityRoleClaimClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityRoleClaimClassGenerator
{
    public string Generate(
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var standardProperties = GetStandardProperties();
        var combined = CombineStandardWithCustom(standardProperties, propertyColumnTypeTriples);

        var sb = new StringBuilder();
        GenerateUsing(sb, config.KeyTypeName);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateClassStart(sb, "IdentityRoleClaimSql", "IIdentityRoleClaimSql");
        GenerateCreateSql(sb, config, combined);
        GenerateDeleteSql(sb, config);
        GenerateGetByRoleIdSql(sb, config);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityRoleClaimCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityRoleClaimDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityRoleClaimGetByRoleIdSql(IdentityDapperConfiguration config);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityRoleClaimCreateSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityRoleClaimDeleteSql(config);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByRoleIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityRoleClaimGetByRoleIdSql(config);
        sb.AppendLine(
            $@"        public string GetByRoleIdSql {{ get; }} =
            @""{content}"";");
    }

    private IList<(string PropertyName, string PropertyType)> GetStandardProperties() =>
        typeof(IdentityRoleClaim<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .Select(p => (PropertyName: p.Name, PropertyType: p.PropertyType.Name))
            .ToList();
}
