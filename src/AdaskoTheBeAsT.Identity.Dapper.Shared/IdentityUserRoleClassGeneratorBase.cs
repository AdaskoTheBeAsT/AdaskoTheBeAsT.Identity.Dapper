using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserRoleClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserRoleClassGenerator
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
        GenerateClassStart(sb, "IdentityUserRoleSql", "IIdentityUserRoleSql");
        GenerateCreateSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateDeleteSql(sb);
        GenerateGetByUserIdRoleIdSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateGetCountSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateGetRoleNamesByUserIdSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserRoleCreateSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserRoleDeleteSql();

    protected abstract string ProcessIdentityUserRoleGetByUserIdRoleIdSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserRoleGetCount(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserRoleGetRoleNamesByUserId(
        IList<string> columnNames,
        IList<string> propertyNames);

    private void GenerateCreateSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserRoleCreateSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb)
    {
        var content = ProcessIdentityUserRoleDeleteSql();
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdRoleIdSql(
        StringBuilder sb,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserRoleGetByUserIdRoleIdSql(columnNames, propertyNames);
        sb.AppendLine(
            $@"        public string GetByUserIdRoleIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetCountSql(
        StringBuilder sb,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserRoleGetCount(columnNames, propertyNames);
        sb.AppendLine(
            $@"        public string GetCountSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetRoleNamesByUserIdSql(
        StringBuilder sb,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserRoleGetRoleNamesByUserId(columnNames, propertyNames);
        sb.AppendLine(
            $@"        public string GetRoleNamesByUserIdSql {{ get; }} =
            @""{content}"";");
    }

    private IList<string> GetStandardPropertyNames() =>
        typeof(IdentityUserLogin<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();
}
