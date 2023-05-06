using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public abstract class IdentityUserRoleClaimClassGeneratorBase
    : IdentityClassGeneratorBase,
        IIdentityUserRoleClaimClassGenerator
{
    public string Generate(IdentityDapperConfiguration config)
    {
        var sb = new StringBuilder();
        GenerateUsing(sb);
        GenerateNamespaceStart(sb, config.NamespaceName);
        GenerateClassStart(sb, "IdentityUserRoleClaimSql", "IIdentityUserRoleClaimSql");
        GenerateGetRoleClaimsByUserIdSql(sb, config);
        GenerateGetUserAndRoleClaimsByUserIdSql(sb, config);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    protected abstract string ProcessIdentityUserRoleClaimGetRoleClaimsByUserIdSql(IdentityDapperConfiguration config);

    protected abstract string ProcessIdentityUserRoleClaimGetUserAndRoleClaimsByUserIdSql(IdentityDapperConfiguration config);

    private void GenerateGetRoleClaimsByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserRoleClaimGetRoleClaimsByUserIdSql(config);
        sb.AppendLine(
            $@"        public string GetRoleClaimsByUserIdSql {{ get; }} =
            @""{content}"";");
        sb.AppendLine();
    }

    private void GenerateGetUserAndRoleClaimsByUserIdSql(
        StringBuilder sb,
        IdentityDapperConfiguration config)
    {
        var content = ProcessIdentityUserRoleClaimGetUserAndRoleClaimsByUserIdSql(config);
        sb.AppendLine(
            $@"        public string GetUserAndRoleClaimsByUserIdSql {{ get; }} =
            @""{content}"";");
    }
}
