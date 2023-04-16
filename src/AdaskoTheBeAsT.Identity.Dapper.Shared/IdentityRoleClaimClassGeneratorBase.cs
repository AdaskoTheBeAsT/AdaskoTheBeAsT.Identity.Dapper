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
        IEnumerable<string> propertyNames,
        IEnumerable<string> columnNames)
    {
        var standardProperties = GetStandardPropertyNames();
        var combinedColumnNames = new List<string>(standardProperties);
        combinedColumnNames.AddRange(columnNames);
        var combinedPropertyNames = new List<string>(standardProperties);
        combinedPropertyNames.AddRange(propertyNames);

        var sb = new StringBuilder();
        GenerateUsing(sb);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateClassStart(sb, "IdentityRoleClaimSql", "IIdentityRoleClaimSql");
        GenerateCreateSql(sb, config, combinedColumnNames, combinedPropertyNames);
        GenerateDeleteSql(sb, config);
        GenerateGetByRoleIdSql(sb, config);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityRoleClaimCreateSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityRoleClaimDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityRoleClaimGetByRoleIdSql(IdentityDapperConfiguration config);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityRoleClaimCreateSql(config, combinedColumnNames, combinedPropertyNames);
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

    private IList<string> GetStandardPropertyNames() =>
        typeof(IdentityRoleClaim<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ToList();
}
