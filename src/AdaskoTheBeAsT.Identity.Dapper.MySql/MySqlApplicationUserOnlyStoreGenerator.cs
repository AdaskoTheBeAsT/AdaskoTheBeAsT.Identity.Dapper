using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlApplicationUserOnlyStoreGenerator
    : IdentityStoreGeneratorBase,
        IApplicationUserOnlyStoreGenerator
{
    private readonly IIdentityHelper _identityHelper;

    public MySqlApplicationUserOnlyStoreGenerator()
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
            "ApplicationUserOnlyStore",
            $"DapperUserOnlyStoreBase<ApplicationUser, {keyTypeName}, ApplicationUserClaim, ApplicationUserLogin, ApplicationUserToken>");
        GenerateConstructor(sb);
        GenerateCreateImpl(sb, keyTypeName);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    private void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            @"        public ApplicationUserOnlyStore(
            IIdentityDbConnectionProvider connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityUserSql(),
                new IdentityUserClaimSql(),
                new IdentityUserLoginSql(),
                new IdentityUserTokenSql())
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
            $@"        protected virtual async Task CreateImplAsync(
            DbConnection connection,
            TUser user,
            CancellationToken cancellationToken)
        {{
            user.Id = ({keyTypeName})(await connection.QueryFirstAsync<ulong>(IdentityUserSql.CreateSql, user).ConfigureAwait(false));
        }}");
    }
}
