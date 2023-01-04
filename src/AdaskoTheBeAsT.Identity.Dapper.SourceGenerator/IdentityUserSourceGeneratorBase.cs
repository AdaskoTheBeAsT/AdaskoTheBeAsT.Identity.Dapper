using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserSourceGeneratorBase
    : IIdentityUserSourceGenerator
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
        var createSqlContent = ProcessIdentityUserCreateSql(combinedColumnNames, combinedPropertyNames);
        var updateSqlContent = ProcessIdentityUserUpdateSql(combinedColumnNames, combinedPropertyNames);
        var sb = new StringBuilder();
        sb.AppendLine(
            $@"using AdaskoTheBeAsT.Identity.Dapper.IdentitySql;

namespace {namespaceName}
{{
    public class IdentityUserSql
        : IIdentityUserSql
    {{");

        sb.AppendLine(
            $@"        public string CreateSql {{ get; }} =
            @""{createSqlContent}"";");

        sb.AppendLine();

        sb.AppendLine(
            $@"        public string UpdateSql {{ get; }} =
            @""{updateSqlContent}"";");

        sb.AppendLine(
            $@"    }}
}}");
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserCreateSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    protected abstract string ProcessIdentityUserUpdateSql(
        IList<string> columnNames,
        IList<string> propertyNames);

    private IList<string> GetStandardPropertyNames() =>
        typeof(IdentityUser<>)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name)
                .ToList();
}
