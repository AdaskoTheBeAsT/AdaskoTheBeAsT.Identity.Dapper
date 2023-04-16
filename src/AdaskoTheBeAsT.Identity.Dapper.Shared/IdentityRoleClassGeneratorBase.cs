using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityRoleClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityRoleClassGenerator
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
        GenerateClassStart(sb, "IdentityRoleSql", "IIdentityRoleSql");
        GenerateCreateSql(sb, config, combinedColumnNames, combinedPropertyNames);
        GenerateUpdateSql(sb, config, combinedColumnNames, combinedPropertyNames);
        GenerateDeleteSql(sb, config);
        GenerateFindByIdSql(sb, config, combinedColumnNames, combinedPropertyNames);
        GenerateFindByNameSql(sb, config, combinedColumnNames, combinedPropertyNames);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityRoleCreateSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityRoleUpdateSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityRoleDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityRoleFindByIdSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityRoleFindByNameSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityRoleCreateSql(config, combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateUpdateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityRoleUpdateSql(config, combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string UpdateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityRoleDeleteSql(config);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityRoleFindByIdSql(config, combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string FindByIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByNameSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityRoleFindByNameSql(config, combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string FindByNameSql {{ get; }} =
            @""{content}"";");
    }

    private IList<string> GetStandardPropertyNames() =>
        typeof(IdentityRole<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ToList();
}
