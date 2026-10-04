using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserLoginClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserLoginClassGenerator
{
    // The established login hooks accept only a schema string, including Delete.
    // Flow the cloned mappings through them without shared mutable per-call state.
    // Saving/restoring also supports reentrant Generate calls from an override.
    private readonly AsyncLocal<IdentityDapperConfiguration?> _generationConfig = new();

    public string Generate(
                IdentityDapperConfiguration config,
                IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var previous = _generationConfig.Value;
        config = config.ForGeneration("IdentityUserLogin", Provider, propertyColumnTypeTriples);
        _generationConfig.Value = config;
        try
        {
            var sb = new StringBuilder();
            GenerateSqlUsing(sb);
            GenerateNamespaceStart(sb, config.NamespaceName);
            GenerateSqlClassStart(sb, "IdentityUserLoginSql", "IIdentityUserLoginSql");
            GenerateCreateSql(sb, config.SchemaPart, propertyColumnTypeTriples);
            GenerateDeleteSql(sb, config.SchemaPart);
            GenerateGetByUserIdSql(sb, config.SchemaPart, propertyColumnTypeTriples);
            GenerateGetByUserIdLoginProviderKeySql(sb, config.SchemaPart, propertyColumnTypeTriples);
            GenerateGetByLoginProviderKeySql(sb, config.SchemaPart, propertyColumnTypeTriples);
            GenerateClassEnd(sb);
            GenerateNamespaceEnd(sb);
            return sb.ToString();
        }
        finally
        {
            _generationConfig.Value = previous;
        }
    }

    public override IList<PropertyColumnTypeTriple> GetAllProperties(
                IEnumerable<PropertyColumnTypeTriple> customs,
                bool insertOwnId) =>
                GetStandardWithCombinedProperties(typeof(IdentityUserLogin<>), insertOwnId, customs);

    internal MappedIdentitySql LoginSql(string schemaPart, IList<PropertyColumnTypeTriple>? properties = null)
    {
        var current = _generationConfig.Value;
        var config = new IdentityDapperConfiguration(
            "IdentityUserLogin",
            current?.KeyTypeName ?? "string",
            current?.NamespaceName ?? string.Empty,
            schemaPart,
            current?.SkipNormalized ?? false,
            current?.InsertOwnId ?? false)
        {
            ColumnMappings = current?.ColumnMappings ??
                new Dictionary<string, IDictionary<string, string>>(StringComparer.Ordinal),
        };
        return Sql(config, properties);
    }

    protected abstract string ProcessIdentityUserLoginCreateSql(
                string schemaPart,
                IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserLoginDeleteSql(string schemaPart);

    protected abstract string ProcessIdentityUserLoginGetByUserIdSql(
                string schemaPart,
                IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(
                string schemaPart,
                IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserLoginGetByLoginProviderKeySql(
                string schemaPart,
                IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    private void GenerateCreateSql(
                StringBuilder sb,
                string schemaPart,
                IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserLoginCreateSql(schemaPart, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string CreateSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
                StringBuilder sb,
                string schemaPart)
    {
        var content = ProcessIdentityUserLoginDeleteSql(schemaPart);
        sb.AppendLine(
            $$"""
                    public string DeleteSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateGetByUserIdSql(
                StringBuilder sb,
                string schemaPart,
                IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserLoginGetByUserIdSql(schemaPart, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string GetByUserIdSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateGetByUserIdLoginProviderKeySql(
                StringBuilder sb,
                string schemaPart,
                IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserLoginGetByUserIdLoginProviderKeySql(schemaPart, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string GetByUserIdLoginProviderKeySql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateGetByLoginProviderKeySql(
                StringBuilder sb,
                string schemaPart,
                IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserLoginGetByLoginProviderKeySql(schemaPart, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string GetByLoginProviderKeySql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
    }
}
