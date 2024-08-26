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
        OracleApplicationUserHelper.GenerateAddClaimsImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateReplaceClaimImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateRemoveClaimsImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateAddLoginImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateRemoveLoginImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateGetLoginsImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateFindUserImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateFindUserLoginImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateFindUserLoginImpl2(sb);
        OracleApplicationUserHelper.GenerateFindByEmailImpl(sb);
        OracleApplicationUserHelper.GenerateGetUsersForClaimImpl(sb);
        OracleApplicationUserHelper.GenerateFindTokenImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateAddUserTokenImpl(sb, keyTypeName);
        OracleApplicationUserHelper.GenerateRemoveUserTokenImpl(sb, keyTypeName);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    private void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            @"        public ApplicationUserOnlyStore(
            IIdentityDbConnectionProvider<OracleConnection> connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityUserSql(),
                new IdentityUserClaimSql(),
                new IdentityUserLoginSql(),
                new IdentityUserTokenSql())
        {
        }");
        sb.AppendLine();
    }
}
