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
        IdentityDapperConfiguration config,
        IEnumerable<PropertyColumnPair> propertyColumnPairs)
    {
        var standardProperties = GetStandardPropertyNames(config.InsertOwnId);
        var combined = CombineStandardWithCustom(standardProperties, propertyColumnPairs);

        var sb = new StringBuilder();
        GenerateUsing(sb);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateClassStart(sb, "IdentityUserSql", "IIdentityUserSql");
        GenerateCreateSql(sb, config, combined);
        GenerateUpdateSql(sb, config, combined);
        GenerateDeleteSql(sb, config);
        GenerateFindByIdSql(sb, config, combined);
        GenerateFindByNameSql(sb, config, combined);
        GenerateFindByEmailSql(sb, config, combined);
        GenerateGetUsersForClaimSql(sb, config, combined);
        GenerateGetUsersInRoleSql(sb, config, combined);
        GenerateGetUsersSql(sb, config, combined);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserUpdateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserFindByIdSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserFindByNameSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserFindByEmailSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserGetUsersForClaimSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserGetUsersInRoleSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserGetUsersSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserCreateSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateUpdateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserUpdateSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string UpdateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserDeleteSql(config);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserFindByIdSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string FindByIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByNameSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserFindByNameSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string FindByNameSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateFindByEmailSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserFindByEmailSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string FindByEmailSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetUsersForClaimSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserGetUsersForClaimSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string GetUsersForClaimSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetUsersInRoleSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserGetUsersInRoleSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string GetUsersInRoleSql {{ get; }} =
            @""{content}"";");
    }

    private void GenerateGetUsersSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserGetUsersSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string GetUsersSql {{ get; }} =
            @""{content}"";");
    }

    private IList<string> GetStandardPropertyNames(bool insertOwnId) =>
        typeof(IdentityUser<>)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => insertOwnId || !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name)
                .ToList();
}
