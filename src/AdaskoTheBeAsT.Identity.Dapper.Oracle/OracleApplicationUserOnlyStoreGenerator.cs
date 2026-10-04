using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleApplicationUserOnlyStoreGenerator
    : OracleIdentityStoreGeneratorBase,
        IApplicationUserOnlyStoreGenerator
{
    public string Generate(
        IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
        IdentityDapperOptions options,
        string keyTypeName,
        string namespaceName,
        bool insertOwnId)
    {
        var sb = new StringBuilder();
        GenerateUsing(sb, keyTypeName);
        GenerateNamespaceStart(sb, namespaceName);
        GenerateClassStart(
            sb,
            "ApplicationUserOnlyStore",
            $"DapperUserOnlyStoreBase<ApplicationUser, {keyTypeName}, ApplicationUserClaim, ApplicationUserLogin, ApplicationUserToken, OracleConnection>");
        GenerateConstructor(sb);
        GenerateUsersProperty(sb);
        GenerateNormalizeSqlMethod(sb);
        OracleApplicationUserHelper.GenerateCreateImpl(
            typePropertiesDict,
            options,
            sb,
            keyTypeName,
            insertOwnId);
        OracleApplicationUserHelper.GenerateUpdateImpl(
            typePropertiesDict,
            options,
            sb,
            keyTypeName);
        OracleApplicationUserHelper.GenerateDeleteImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateFindByIdImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateFindByNameImpl(sb);
        OracleApplicationUserHelper.GenerateGetClaimsImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateClaimBatchParameters(sb, keyTypeName, typePropertiesDict["IdentityUserClaim"], options);
        OracleApplicationUserHelper.GenerateReplaceClaimImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateAddLoginImpl(sb, keyTypeName, typePropertiesDict["IdentityUserLogin"], options);
        OracleApplicationUserHelper.GenerateRemoveLoginImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateGetLoginsImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateFindUserImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateFindUserLoginImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateFindUserLoginImpl2(sb);
        OracleApplicationUserHelper.GenerateFindByEmailImpl(sb);
        OracleApplicationUserHelper.GenerateGetUsersForClaimImpl(sb);
        OracleApplicationUserHelper.GenerateFindTokenImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateAddUserTokenImpl(sb, keyTypeName, typePropertiesDict["IdentityUserToken"], options);
        OracleApplicationUserHelper.GenerateTryUpdateTokenImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateRemoveUserTokenImpl(sb, keyTypeName);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    private static void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            """
                    public ApplicationUserOnlyStore(
                        IIdentityDbConnectionProvider<OracleConnection> connectionProvider)
                        : base(
                            new IdentityErrorDescriber(),
                            connectionProvider,
                            new IdentityUserSql(),
                            new IdentityUserClaimSql(),
                            new IdentityUserLoginSql(),
                            new IdentityUserTokenSql())
                    {
                    }
            """);
        sb.AppendLine();
    }

    private static void GenerateUsersProperty(StringBuilder sb)
    {
        sb.AppendLine(
            """
                    public override IQueryable<ApplicationUser> Users
                    {
                        get
                        {
                            ThrowIfDisposed();
                            using var connection = ConnectionProvider.Provide();
                            return connection.QueryIdentity<ApplicationUser>(NormalizeSql(IdentityUserSql.GetUsersSql)).AsQueryable();
                        }
                    }
            """);
        sb.AppendLine();
    }
}
