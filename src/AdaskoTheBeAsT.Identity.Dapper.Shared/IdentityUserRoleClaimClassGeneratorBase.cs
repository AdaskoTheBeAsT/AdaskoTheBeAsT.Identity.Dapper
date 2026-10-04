using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserRoleClaimClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserRoleClaimClassGenerator
{
    public string Generate(IdentityDapperConfiguration config)
    {
        config = config.ForGeneration("IdentityUserRoleClaim", Provider, new List<PropertyColumnTypeTriple>());
        var sb = new StringBuilder();
        GenerateSqlUsing(sb);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateSqlClassStart(sb, "IdentityUserRoleClaimSql", "IIdentityUserRoleClaimSql");
        GenerateGetRoleClaimsByUserIdSql(sb, config);
        GenerateGetUserAndRoleClaimsByUserIdSql(sb, config);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    public override IList<PropertyColumnTypeTriple> GetAllProperties(
        IEnumerable<PropertyColumnTypeTriple> customs,
        bool insertOwnId) => [];

    protected abstract string ProcessIdentityUserRoleClaimGetRoleClaimsByUserIdSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserRoleClaimGetUserAndRoleClaimsByUserIdSql(IdentityDapperConfiguration config);

    private void GenerateGetRoleClaimsByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserRoleClaimGetRoleClaimsByUserIdSql(config);
        sb.Append("        public string GetRoleClaimsByUserIdSql { get; } =\r\n            ").Append(RawStringLiteral.Format(content)).Append(';').AppendLine();
        sb.AppendLine();
    }

    private void GenerateGetUserAndRoleClaimsByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserRoleClaimGetUserAndRoleClaimsByUserIdSql(config);
        sb.Append("        public string GetUserAndRoleClaimsByUserIdSql { get; } =\r\n            ").Append(RawStringLiteral.Format(content)).Append(';').AppendLine();
    }
}
