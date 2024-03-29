using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerApplicationRoleStoreGenerator
    : IdentityStoreGeneratorBase,
        IApplicationRoleStoreGenerator
{
    public string Generate(string keyTypeName, string namespaceName)
    {
        var sb = new StringBuilder();
        GenerateUsing(sb);
        GenerateNamespaceStart(sb, namespaceName);
        GenerateClassStart(
            sb,
            "ApplicationRoleStore",
            $"DapperRoleStoreBase<ApplicationRole, {keyTypeName}, ApplicationRoleClaim>");
        GenerateConstructor(sb);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    private void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            @"        public ApplicationRoleStore(
            IIdentityDbConnectionProvider connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityRoleSql(),
                new IdentityRoleClaimSql())
        {
        }");
    }
}
