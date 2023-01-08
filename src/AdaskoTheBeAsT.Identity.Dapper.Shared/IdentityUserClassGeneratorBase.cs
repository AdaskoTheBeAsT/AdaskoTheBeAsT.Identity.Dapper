using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserClassGenerator
{
    public string Generate(
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
        GenerateClassStart(sb, "IdentityUserSql", "IIdentityUserSql");
        GenerateCreateSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateUpdateSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateDeleteSql(sb);
        GenerateFindByIdSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateFindByNameSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateFindByEmailSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateGetUsersForClaimSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateGetUsersInRoleSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserCreateSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserUpdateSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserDeleteSql();

    protected abstract string ProcessIdentityUserFindByIdSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserFindByNameSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserFindByEmailSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserGetUsersForClaimSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserGetUsersInRoleSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    private void GenerateCreateSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserCreateSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateUpdateSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserUpdateSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string UpdateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb)
    {
        var content = ProcessIdentityUserDeleteSql();
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByIdSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserFindByIdSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string FindByIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByNameSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserFindByNameSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string FindByNameSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByEmailSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserFindByEmailSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string FindByEmailSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetUsersForClaimSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserGetUsersForClaimSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string GetUsersForClaimSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetUsersInRoleSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserGetUsersInRoleSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string GetUsersInRoleSql {{ get; }} =
            @""{content}"";");
    }

    private IList<string> GetStandardPropertyNames() =>
        typeof(IdentityUser<>)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name)
                .ToList();
}
