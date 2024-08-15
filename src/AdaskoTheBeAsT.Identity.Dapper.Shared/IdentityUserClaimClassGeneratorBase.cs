using System.Collections.Generic;
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
        var sb = new StringBuilder();
        GenerateUsing(sb, config.KeyTypeName);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateClassStart(sb, "IdentityUserClaimSql", "IIdentityUserClaimSql");
        GenerateCreateSql(sb, config, propertyColumnTypeTriples);
        GenerateDeleteSql(sb, config);
        GenerateGetByUserIdSql(sb, config);
        GenerateReplaceSql(sb, config, propertyColumnTypeTriples);
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
        IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
    {
        var content = ProcessIdentityUserClaimReplaceSql(config, propertyColumnTypeTriples);
        sb.AppendLine(
            $@"        public string ReplaceSql {{ get; }} =
            @""{content}"";");
    }
}
