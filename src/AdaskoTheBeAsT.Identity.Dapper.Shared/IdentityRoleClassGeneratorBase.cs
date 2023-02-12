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
        string schemaPart,
        string keyTypeName,
        string namespaceName,
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
        GenerateNamespaceStart(sb, namespaceName);
        GenerateClassStart(sb, "IdentityRoleSql", "IIdentityRoleSql");
        GenerateCreateSql(sb, schemaPart, keyTypeName, combinedColumnNames, combinedPropertyNames);
        GenerateUpdateSql(sb, schemaPart, combinedColumnNames, combinedPropertyNames);
        GenerateDeleteSql(sb, schemaPart);
        GenerateFindByIdSql(sb, schemaPart, combinedColumnNames, combinedPropertyNames);
        GenerateFindByNameSql(sb, schemaPart, combinedColumnNames, combinedPropertyNames);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityRoleCreateSql(
        string schemaPart,
        string keyTypeName,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityRoleUpdateSql(
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityRoleDeleteSql(string schemaPart);

    protected abstract string ProcessIdentityRoleFindByIdSql(
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityRoleFindByNameSql(
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames);

    private void GenerateCreateSql(
        StringBuilder sb,
        string schemaPart,
        string keyTypeName,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityRoleCreateSql(schemaPart, keyTypeName, combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateUpdateSql(
        StringBuilder sb,
        string schemaPart,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityRoleUpdateSql(schemaPart, combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string UpdateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        string schemaPart)
    {
        var content = ProcessIdentityRoleDeleteSql(schemaPart);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByIdSql(
        StringBuilder sb,
        string schemaPart,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityRoleFindByIdSql(schemaPart, combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string FindByIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByNameSql(
        StringBuilder sb,
        string schemaPart,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityRoleFindByNameSql(schemaPart, combinedColumnNames, combinedPropertyNames);
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
