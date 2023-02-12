using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserLoginClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserLoginClassGenerator
{
    public string Generate(
        string schemaPart,
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
        GenerateClassStart(sb, "IdentityUserLoginSql", "IIdentityUserLoginSql");
        GenerateCreateSql(sb, schemaPart, combinedColumnNames, combinedPropertyNames);
        GenerateDeleteSql(sb, schemaPart);
        GenerateGetByUserIdSql(sb, schemaPart, combinedColumnNames, combinedPropertyNames);
        GenerateGetByUserIdLoginProviderKeySql(sb, schemaPart, combinedColumnNames, combinedPropertyNames);
        GenerateGetByLoginProviderKeySql(sb, schemaPart, combinedColumnNames, combinedPropertyNames);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserLoginCreateSql(
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserLoginDeleteSql(string schemaPart);

    protected abstract string ProcessIdentityUserLoginGetByUserIdSql(
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserLoginGetByLoginProviderKeySql(
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames);

    private void GenerateCreateSql(
        StringBuilder sb,
        string schemaPart,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserLoginCreateSql(schemaPart, combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        string schemaPart)
    {
        var content = ProcessIdentityUserLoginDeleteSql(schemaPart);
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdSql(
        StringBuilder sb,
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserLoginGetByUserIdSql(schemaPart, columnNames, propertyNames);
        sb.AppendLine(
            $@"        public string GetByUserIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdLoginProviderKeySql(
        StringBuilder sb,
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(schemaPart, columnNames, propertyNames);
        sb.AppendLine(
            $@"        public string GetByUserIdLoginProviderKeySql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByLoginProviderKeySql(
        StringBuilder sb,
        string schemaPart,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserLoginGetByLoginProviderKeySql(schemaPart, columnNames, propertyNames);
        sb.AppendLine(
            $@"        public string GetByLoginProviderKeySql {{ get; }} =
            @""{content}"";");
    }

    private IList<string> GetStandardPropertyNames() =>
        typeof(IdentityUserLogin<>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();
}
