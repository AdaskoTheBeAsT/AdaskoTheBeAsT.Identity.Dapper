using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlApplicationRoleStoreGenerator
    : IdentityStoreGeneratorBase,
        IApplicationRoleStoreGenerator
{
    private readonly IIdentityHelper _identityHelper;

    public MySqlApplicationRoleStoreGenerator()
    {
        _identityHelper = new MySqlIdentityHelper();
    }

    public string Generate(
        string keyTypeName,
        string namespaceName)
    {
        var sb = new StringBuilder();
        GenerateUsing(sb);
        GenerateNamespaceStart(sb, namespaceName);
        GenerateClassStart(
            sb,
            "ApplicationRoleStore",
            $"DapperRoleStoreBase<ApplicationRole, {keyTypeName}, ApplicationRoleClaim>");
        GenerateConstructor(sb);
        GenerateCreateImpl(sb, keyTypeName);
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

    private void GenerateCreateImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        if (!_identityHelper.NumberNameSet.Contains(keyTypeName))
        {
            return;
        }

        sb.AppendLine(
            $@"        protected override async Task CreateImplAsync(
            IDbConnection connection,
            TRole role,
            CancellationToken cancellationToken)
        {{
            role.Id = ({keyTypeName})(await connection.QueryFirstAsync<ulong>(IdentityRoleSql.CreateSql, role).ConfigureAwait(false));
        }}");
    }
}
