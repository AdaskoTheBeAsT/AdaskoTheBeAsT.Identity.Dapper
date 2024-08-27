using System.Collections.Generic;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleApplicationUserStoreGenerator
    : OracleIdentityStoreGeneratorBase,
        IApplicationUserStoreGenerator
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
            "ApplicationUserStore",
            $"DapperUserStoreBase<ApplicationUser, ApplicationRole, {keyTypeName}, ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin, ApplicationUserToken, OracleConnection>");
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
        GenerateGetUsersInRoleImpl(sb);
        GenerateAddToRoleImpl(sb, keyTypeName);
        GenerateRemoveFromRoleImpl(sb, keyTypeName);
        GenerateGetRolesImpl(sb, keyTypeName);
        GenerateIsInRoleImpl(sb, keyTypeName);
        GenerateGetRoleClaimsImpl(sb, keyTypeName);
        GenerateGetUserAndRoleClaimsImpl(sb, keyTypeName);
        GenerateFindRoleImpl(sb);
        GenerateFindUserRole(sb, keyTypeName);
        GenerateClassEnd(sb);
        GenerateNamespaceEnd(sb);
        return sb.ToString();
    }

    private void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            @"        public ApplicationUserStore(
            IIdentityDbConnectionProvider<OracleConnection> connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityUserSql(),
                new IdentityUserClaimSql(),
                new IdentityUserLoginSql(),
                new IdentityUserTokenSql(),
                new IdentityUserRoleSql(),
                new IdentityRoleSql(),
                new IdentityUserRoleClaimSql())
        {
        }");
        sb.AppendLine();
    }

    private void GenerateGetUsersInRoleImpl(
        StringBuilder sb)
    {
        sb.AppendLine(
            $@"        protected override async Task<IList<ApplicationUser>> GetUsersInRoleImplAsync(
            OracleConnection connection,
            string roleName,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserSql.GetUsersInRoleSql;
            var parameters = new OracleDynamicParameters();");

        sb.AppendLine(
            $@"            parameters.Add(""NormalizedName"", roleName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            $@"            return (await connection.QueryAsync<ApplicationUser>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateAddToRoleImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task AddToRoleImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserRoleSql.CreateSql;
            var parameters = new OracleDynamicParameters();");
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""UserId"", user.Id, {idType}, ParameterDirection.Input, {idSize});
            parameters.Add(""RoleId"", role.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateRemoveFromRoleImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task RemoveFromRoleImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserRoleSql.DeleteSql;
            var parameters = new OracleDynamicParameters();");
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""UserId"", user.Id, {idType}, ParameterDirection.Input, {idSize});
            parameters.Add(""RoleId"", role.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateGetRolesImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<IList<string>> GetRolesImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserRoleSql.GetRoleNamesByUserIdSql;
            var parameters = new OracleDynamicParameters();");
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""UserId"", user.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return (await connection.QueryAsync<string>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateIsInRoleImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<bool> IsInRoleImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserRoleSql.GetCountSql;
            var parameters = new OracleDynamicParameters();");
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""UserId"", user.Id, {idType}, ParameterDirection.Input, {idSize});
            parameters.Add(""RoleId"", role.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return (await connection.QueryFirstOrDefaultAsync<int>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false)) > 0;");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateGetRoleClaimsImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<IList<Claim>> GetRoleClaimsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserRoleClaimSql.GetRoleClaimsByUserIdSql;
            var parameters = new OracleDynamicParameters();");
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""Id"", user.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return (await connection.QueryAsync<Claim>(sql, parameters)
                        .ConfigureAwait(continueOnCapturedContext: false))
                    .AsList();");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateGetUserAndRoleClaimsImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<IList<Claim>> GetUserAndRoleClaimsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserRoleClaimSql.GetUserAndRoleClaimsByUserIdSql;
            var parameters = new OracleDynamicParameters();");
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""Id"", user.Id, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return (await connection.QueryAsync<Claim>(sql, parameters)
                        .ConfigureAwait(continueOnCapturedContext: false))
                    .AsList();");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateFindRoleImpl(
        StringBuilder sb)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationRole?> FindRoleImplAsync(
            OracleConnection connection,
            string roleName,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityRoleSql.FindByNameSql;
            var parameters = new OracleDynamicParameters();");
        sb.AppendLine(
            $@"            parameters.Add(""NormalizedName"", roleName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationRole>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private void GenerateFindUserRole(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            $@"        protected override async Task<ApplicationUserRole?> FindUserRoleAsync(
            OracleConnection connection,
            {keyTypeName} userId,
            {keyTypeName} roleId,
            CancellationToken cancellationToken)
        {{
            var sql = IdentityUserRoleSql.GetByUserIdRoleIdSql;
            var parameters = new OracleDynamicParameters();");
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $@"            parameters.Add(""UserId"", userId, {idType}, ParameterDirection.Input, {idSize});
            parameters.Add(""RoleId"", roleId, {idType}, ParameterDirection.Input, {idSize});");

        sb.AppendLine(
            $@"            return await connection.QueryFirstOrDefaultAsync<ApplicationUserRole>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);");

        sb.AppendLine("        }");

        sb.AppendLine();
    }
}
