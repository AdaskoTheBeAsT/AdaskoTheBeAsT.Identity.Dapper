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
        GenerateClassStart(sb, "IdentityUserClaimSql", "IIdentityUserClaimSql");
        GenerateCreateSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateDeleteSql(sb);
        GenerateGetByUserIdSql(sb);
        GenerateReplaceSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserClaimCreateSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserClaimDeleteSql();

    protected abstract string ProcessIdentityUserClaimGetByUserIdSql();

    protected abstract string ProcessIdentityUserClaimReplaceSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    private void GenerateCreateSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserClaimCreateSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb)
    {
        var content = ProcessIdentityUserClaimDeleteSql();
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdSql(
        StringBuilder sb)
    {
        var content = ProcessIdentityUserClaimGetByUserIdSql();
        sb.AppendLine(
            $@"        public string GetByUserIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateReplaceSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserClaimReplaceSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string ReplaceSql {{ get; }} =
            @""{content}"";");
    }

    private IList<string> GetStandardPropertyNames() =>
        typeof(IdentityUserClaim<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ToList();
}
