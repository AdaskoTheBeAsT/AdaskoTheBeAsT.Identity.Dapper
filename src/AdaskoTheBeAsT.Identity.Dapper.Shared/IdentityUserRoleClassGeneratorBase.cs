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
        IEnumerable<PropertyColumnPair> propertyColumnPairs)
    {
        var standardProperties = GetStandardPropertyNames();
        var combined = CombineStandardWithCustom(standardProperties, propertyColumnPairs);

        var sb = new StringBuilder();
        GenerateUsing(sb);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateClassStart(sb, "IdentityUserRoleSql", "IIdentityUserRoleSql");
        GenerateCreateSql(sb, config, combined);
        GenerateDeleteSql(sb, config);
        GenerateGetByUserIdRoleIdSql(sb, config, combined);
        GenerateGetCountSql(sb, config, combined);
        GenerateGetRoleNamesByUserIdSql(sb, config, combined);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserRoleCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserRoleDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserRoleGetByUserIdRoleIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserRoleGetCount(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserRoleGetRoleNamesByUserId(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserRoleCreateSql(config, propertyColumnPairs);
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
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserRoleGetByUserIdRoleIdSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string GetByUserIdRoleIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetCountSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserRoleGetCount(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string GetCountSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetRoleNamesByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserRoleGetRoleNamesByUserId(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string GetRoleNamesByUserIdSql {{ get; }} =
            @""{content}"";");
    }

    private IList<string> GetStandardPropertyNames() =>
        typeof(IdentityUserRole<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();
}
