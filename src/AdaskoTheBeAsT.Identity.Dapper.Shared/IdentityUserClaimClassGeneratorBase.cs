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
        var names = string.Join(", ", propertyColumnTypeTriples.Select(p => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(p.PropertyName, quote: true)));
        sb.AppendLine();
        sb.Append("        public string CreateBatchItemSql { get; } =\r\n            ").Append(RawStringLiteral.Format(sql.CreateClaimBatchItem())).Append(";\r\n\r\n        public string DeleteBatchItemSql { get; } =\r\n            ").Append(RawStringLiteral.Format(sql.DeleteClaimBatchItem())).Append(";\r\n\r\n        public System.Collections.Generic.IReadOnlyList<string> CreateBatchParameterNames { get; } = new[] { ").Append(names).Append(" };\r\n\r\n        public string BatchPrefix { get; } = ").Append(Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(Provider == DatabaseProvider.Oracle ? "BEGIN\n" : string.Empty, quote: true)).Append(";\r\n\r\n        public string BatchSuffix { get; } = ").Append(Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(Provider == DatabaseProvider.Oracle ? "\nEND;" : string.Empty, quote: true)).Append(';').AppendLine();
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
        sb.Append("        public string CreateSql { get; } =\r\n            ").Append(RawStringLiteral.Format(content)).Append(';').AppendLine();
        sb.AppendLine();
    }

    private void GenerateDeleteSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserClaimDeleteSql(config);
        sb.Append("        public string DeleteSql { get; } =\r\n            ").Append(RawStringLiteral.Format(content)).Append(';').AppendLine();
        sb.AppendLine();
    }

    private void GenerateGetByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserClaimGetByUserIdSql(config);
        sb.Append("        public string GetByUserIdSql { get; } =\r\n            ").Append(RawStringLiteral.Format(content)).Append(';').AppendLine();
        sb.AppendLine();
    }

    private void GenerateReplaceSql(
        StringBuilder sb,
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserClaimReplaceSql(config, propertyColumnTypeTriples);
        sb.Append("        public string ReplaceSql { get; } =\r\n            ").Append(RawStringLiteral.Format(content)).Append(';').AppendLine();
    }
}
