using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserClaimClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserClaimClassGenerator
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
        GenerateClassStart(sb, "IdentityUserClaimSql", "IIdentityUserClaimSql");
        GenerateCreateSql(sb, config, combined);
        GenerateDeleteSql(sb, config);
        GenerateGetByUserIdSql(sb, config);
        GenerateReplaceSql(sb, config, combined);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserClaimCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    protected abstract string ProcessIdentityUserClaimDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserClaimGetByUserIdSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserClaimReplaceSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserClaimCreateSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserClaimDeleteSql(config);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserClaimGetByUserIdSql(config);
        sb.AppendLine(
            $@"        public string GetByUserIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateReplaceSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnPair> propertyColumnPairs)
    {
        var content = ProcessIdentityUserClaimReplaceSql(config, propertyColumnPairs);
        sb.AppendLine(
            $@"        public string ReplaceSql {{ get; }} =
            @""{content}"";");
    }

    private IList<string> GetStandardPropertyNames(bool insertOwnId) =>
        typeof(IdentityUserClaim<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => insertOwnId || !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ToList();
}
