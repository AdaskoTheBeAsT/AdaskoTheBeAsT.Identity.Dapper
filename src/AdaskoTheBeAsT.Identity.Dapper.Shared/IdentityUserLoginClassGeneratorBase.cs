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
        GenerateCreateSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateDeleteSql(sb);
        GenerateGetByUserIdSql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateGetByUserIdLoginProviderKeySql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateGetByLoginProviderKeySql(sb, combinedColumnNames, combinedPropertyNames);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserLoginCreateSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserLoginDeleteSql();

    protected abstract string ProcessIdentityUserLoginGetByUserIdSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserLoginGetByLoginProviderKeySql(
        IList<string> columnNames,
        IList<string> propertyNames);

    private void GenerateCreateSql(
        StringBuilder sb,
        IList<string> combinedColumnNames,
        IList<string> combinedPropertyNames)
    {
        var content = ProcessIdentityUserLoginCreateSql(combinedColumnNames, combinedPropertyNames);
        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb)
    {
        var content = ProcessIdentityUserLoginDeleteSql();
        sb.AppendLine(
            $@"        public string DeleteSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdSql(
        StringBuilder sb,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserLoginGetByUserIdSql(columnNames, propertyNames);
        sb.AppendLine(
            $@"        public string GetByUserIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByUserIdLoginProviderKeySql(
        StringBuilder sb,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(columnNames, propertyNames);
        sb.AppendLine(
            $@"        public string GetByUserIdLoginProviderKeySql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetByLoginProviderKeySql(
        StringBuilder sb,
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var content = ProcessIdentityUserLoginGetByLoginProviderKeySql(columnNames, propertyNames);
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
