using System.Collections.Generic;
using System.Linq;
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
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        config = config.ForGeneration("IdentityUserClaim", Provider, propertyColumnTypeTriples);
        var sb = new StringBuilder();
        GenerateSqlUsing(sb);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateSqlClassStart(sb, "IdentityUserClaimSql", "IIdentityUserClaimBatchSql");
        GenerateCreateSql(sb, config, propertyColumnTypeTriples);
        GenerateDeleteSql(sb, config);
        GenerateGetByUserIdSql(sb, config);
        GenerateReplaceSql(sb, config, propertyColumnTypeTriples);
        var sql = Sql(config, propertyColumnTypeTriples);
        var names = string.Join(", ", propertyColumnTypeTriples.Select(p => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(p.PropertyName, true)));
        sb.AppendLine();
        sb.AppendLine(
            $$"""
                    public string CreateBatchItemSql { get; } =
                        {{RawStringLiteral.Format(sql.CreateClaimBatchItem())}};

                    public string DeleteBatchItemSql { get; } =
                        {{RawStringLiteral.Format(sql.DeleteClaimBatchItem())}};

                    public System.Collections.Generic.IReadOnlyList<string> CreateBatchParameterNames { get; } = new[] { {{names}} };

                    public string BatchPrefix { get; } = {{Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(Provider == DatabaseProvider.Oracle ? "BEGIN\n" : string.Empty, true)}};

                    public string BatchSuffix { get; } = {{Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(Provider == DatabaseProvider.Oracle ? "\nEND;" : string.Empty, true)}};
            """);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    public override IList<PropertyColumnTypeTriple> GetAllProperties(
        IEnumerable<PropertyColumnTypeTriple> customs,
        bool insertOwnId) =>
        GetStandardWithCombinedProperties(typeof(IdentityUserClaim<>), insertOwnId, customs);

    protected abstract string ProcessIdentityUserClaimCreateSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    protected abstract string ProcessIdentityUserClaimDeleteSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserClaimGetByUserIdSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserClaimReplaceSql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples);

    private void GenerateCreateSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserClaimCreateSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string CreateSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserClaimDeleteSql(config);
        sb.AppendLine(
            $$"""
                    public string DeleteSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateGetByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserClaimGetByUserIdSql(config);
        sb.AppendLine(
            $$"""
                    public string GetByUserIdSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
        sb.AppendLine();
    }

    private void GenerateReplaceSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserClaimReplaceSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $$"""
                    public string ReplaceSql { get; } =
                        {{RawStringLiteral.Format(content)}};
            """);
    }
}
