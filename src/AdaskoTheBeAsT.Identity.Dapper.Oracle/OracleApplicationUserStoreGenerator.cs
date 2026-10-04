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
        GenerateGetUsersInRoleImpl(sb);
        GenerateAddToRoleImpl(sb, keyTypeName, typePropertiesDict["IdentityUserRole"], options);
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

    private static void GenerateConstructor(StringBuilder sb)
    {
        sb.AppendLine(
            """
                    public ApplicationUserStore(
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

    private static void GenerateGetUsersInRoleImpl(
        StringBuilder sb)
    {
        sb.AppendLine(
            """
                    protected override async Task<IList<ApplicationUser>> GetUsersInRoleImplAsync(
                        OracleConnection connection,
                        string roleName,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserSql.GetUsersInRoleSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        sb.AppendLine(
            """            parameters.Add("NormalizedName", roleName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);""");

        sb.AppendLine(
            """
                        return (await connection.QueryIdentityAsync<ApplicationUser>(sql, parameters, cancellationToken)
                                .ConfigureAwait(continueOnCapturedContext: false))
                            .AsList();
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private static void GenerateAddToRoleImpl(
        StringBuilder sb,
        string keyTypeName,
        IList<PropertyColumnTypeTriple> properties,
        IdentityDapperOptions options)
    {
        sb.AppendLine(
            """
                    protected override async Task AddToRoleImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        ApplicationRole role,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserRoleSql.CreateSql);
                        var entity = CreateUserRole(user, role);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);
        OracleApplicationUserHelper.GenerateEntityParameters(sb, properties, "entity", keyTypeName, options);

        sb.AppendLine(
            """
                        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private static void GenerateRemoveFromRoleImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task RemoveFromRoleImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        ApplicationRole role,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserRoleSql.DeleteSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"UserId\", user.Id, ").Append(idType).Append(", ParameterDirection.Input, ").Append(idSize).Append(");\r\n            parameters.Add(\"RoleId\", role.Id, ").Append(idType).Append(", ParameterDirection.Input, ").Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private static void GenerateGetRolesImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task<IList<string>> GetRolesImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserRoleSql.GetRoleNamesByUserIdSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"UserId\", user.Id, ").Append(idType).Append(", ParameterDirection.Input, ").Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return (await connection.QueryAsync<string>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                .ConfigureAwait(continueOnCapturedContext: false))
                            .AsList();
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private static void GenerateIsInRoleImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task<bool> IsInRoleImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        ApplicationRole role,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserRoleSql.GetCountSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"UserId\", user.Id, ").Append(idType).Append(", ParameterDirection.Input, ").Append(idSize).Append(");\r\n            parameters.Add(\"RoleId\", role.Id, ").Append(idType).Append(", ParameterDirection.Input, ").Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return (await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                .ConfigureAwait(continueOnCapturedContext: false)) > 0;
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private static void GenerateGetRoleClaimsImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task<IList<Claim>> GetRoleClaimsImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserRoleClaimSql.GetRoleClaimsByUserIdSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"Id\", user.Id, ").Append(idType).Append(", ParameterDirection.Input, ").Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return (await connection.QueryAsync<Claim>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                    .ConfigureAwait(continueOnCapturedContext: false))
                                .AsList();
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private static void GenerateGetUserAndRoleClaimsImpl(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task<IList<Claim>> GetUserAndRoleClaimsImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserRoleClaimSql.GetUserAndRoleClaimsByUserIdSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"Id\", user.Id, ").Append(idType).Append(", ParameterDirection.Input, ").Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return (await connection.QueryAsync<Claim>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                    .ConfigureAwait(continueOnCapturedContext: false))
                                .AsList();
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private static void GenerateFindRoleImpl(
        StringBuilder sb)
    {
        sb.AppendLine(
            """
                    protected override async Task<ApplicationRole?> FindRoleImplAsync(
                        OracleConnection connection,
                        string roleName,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityRoleSql.FindByNameSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);
        sb.AppendLine(
            """            parameters.Add("NormalizedName", roleName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);""");

        sb.AppendLine(
            """
                        return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationRole>(sql, parameters, cancellationToken)
                                .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    private static void GenerateFindUserRole(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.Append("        protected override async Task<ApplicationUserRole?> FindUserRoleAsync(\r\n            OracleConnection connection,\r\n            ").Append(keyTypeName).Append(" userId,\r\n            ").Append(keyTypeName).AppendLine(" roleId,\r\n            CancellationToken cancellationToken)\r\n        {\r\n            var sql = NormalizeSql(IdentityUserRoleSql.GetByUserIdRoleIdSql);\r\n            var parameters = new OracleDynamicParameters { BindByName = true };");
        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"UserId\", userId, ").Append(idType).Append(", ParameterDirection.Input, ").Append(idSize).Append(");\r\n            parameters.Add(\"RoleId\", roleId, ").Append(idType).Append(", ParameterDirection.Input, ").Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return await connection.QueryFirstOrDefaultAsync<ApplicationUserRole>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }
}
