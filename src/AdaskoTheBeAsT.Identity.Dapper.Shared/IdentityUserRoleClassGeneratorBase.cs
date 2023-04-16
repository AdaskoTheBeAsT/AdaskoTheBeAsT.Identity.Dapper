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
        GenerateClassStart(sb, "IdentityUserRoleSql", "IIdentityUserRoleSql");
        GenerateCreateSql(sb, config, combinedColumnNames, combinedPropertyNames);
        GenerateDeleteSql(sb, config);
        GenerateGetByUserIdRoleIdSql(sb, config, combinedColumnNames, combinedPropertyNames);
        GenerateGetCountSql(sb, config, combinedColumnNames, combinedPropertyNames);
        GenerateGetRoleNamesByUserIdSql(sb, config, combinedColumnNames, combinedPropertyNames);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserRoleCreateSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserRoleDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserRoleGetByUserIdRoleIdSql(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserRoleGetCount(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserRoleGetRoleNamesByUserId(
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserRoleCreateSql(config, combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserRoleDeleteSql(config);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdRoleIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserRoleGetByUserIdRoleIdSql(config, columnNames, propertyNames);
        sb.AppendLine(
            $@"        public string GetByUserIdRoleIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetCountSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserRoleGetCount(config, columnNames, propertyNames);
        sb.AppendLine(
            $@"        public string GetCountSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetRoleNamesByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserRoleGetRoleNamesByUserId(config, columnNames, propertyNames);
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
