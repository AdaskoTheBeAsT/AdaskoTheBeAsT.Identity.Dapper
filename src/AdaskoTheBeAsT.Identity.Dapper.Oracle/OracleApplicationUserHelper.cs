using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public static class OracleApplicationUserHelper
{
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(IdentityUser<int>.Id),
        nameof(IdentityUser<int>.UserName),
        nameof(IdentityUser<int>.NormalizedUserName),
        nameof(IdentityUser<int>.Email),
        nameof(IdentityUser<int>.NormalizedEmail),
        nameof(IdentityUser<int>.EmailConfirmed),
        nameof(IdentityUser<int>.PasswordHash),
        nameof(IdentityUser<int>.SecurityStamp),
        nameof(IdentityUser<int>.ConcurrencyStamp),
        nameof(IdentityUser<int>.PhoneNumber),
        nameof(IdentityUser<int>.PhoneNumberConfirmed),
        nameof(IdentityUser<int>.TwoFactorEnabled),
        nameof(IdentityUser<int>.LockoutEnd),
        nameof(IdentityUser<int>.LockoutEnabled),
        nameof(IdentityUser<int>.AccessFailedCount),
    };

    public static void GenerateCreateImpl(
                IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
                IdentityDapperOptions options,
                StringBuilder sb,
                string keyTypeName,
                bool insertOwnId)
    {
        OracleStoreMethodGenerator.GenerateCreateStart(sb, "User", "user", keyTypeName, insertOwnId);
        GenerateUserParameters(sb, options, "user.ConcurrencyStamp");
        OracleStoreMethodGenerator.GenerateCustomParameters(
            sb, typePropertiesDict, nameof(IdentityUser<int>), ExcludedProperties, "user", options);
        OracleStoreMethodGenerator.GenerateOutputId(sb, "user", keyTypeName);
    }

    public static void GenerateUpdateImpl(
                IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
                IdentityDapperOptions options,
                StringBuilder sb,
                string keyTypeName)
    {
        OracleStoreMethodGenerator.GenerateUpdateStart(sb, "User", "user", keyTypeName);
        GenerateUserParameters(sb, options, "stamp");
        OracleStoreMethodGenerator.GenerateCustomParameters(
            sb, typePropertiesDict, nameof(IdentityUser<int>), ExcludedProperties, "user", options);

        sb.AppendLine(
            """
                        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
                        if (affected != 1)
                        {
                            throw new DBConcurrencyException();
                        }

                        user.ConcurrencyStamp = stamp;
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateDeleteImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task DeleteImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserSql.DeleteSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
                        parameters.Add("ConcurrencyStamp", user.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            """);

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);

        sb.Append("            parameters.Add(\"Id\", user.Id, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
                        if (affected != 1)
                        {
                            throw new DBConcurrencyException();
                        }
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindByIdImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.Append("        protected override async Task<ApplicationUser?> FindByIdImplAsync(\r\n            OracleConnection connection,\r\n            ").Append(keyTypeName).AppendLine(" userId,\r\n            CancellationToken cancellationToken)\r\n        {\r\n            var sql = NormalizeSql(IdentityUserSql.FindByIdSql);\r\n            var parameters = new OracleDynamicParameters { BindByName = true };");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"Id\", userId, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationUser>(sql, parameters, cancellationToken)
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindByNameImpl(
                StringBuilder sb)
    {
        sb.AppendLine(
            """
                    protected override async Task<ApplicationUser?> FindByNameImplAsync(
                        OracleConnection connection,
                        string normalizedUserName,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserSql.FindByNameSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        sb.AppendLine(
            """            parameters.Add("NormalizedUserName", normalizedUserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);""");

        sb.AppendLine(
            """
                        return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationUser>(sql, parameters, cancellationToken)
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateGetClaimsImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.Append("        protected override async Task<IList<Claim>> GetClaimsImplAsync(\r\n            OracleConnection connection,\r\n            ").Append(keyTypeName).AppendLine(" userId,\r\n            CancellationToken cancellationToken)\r\n        {\r\n            var sql = NormalizeSql(IdentityUserClaimSql.GetByUserIdSql);\r\n            var parameters = new OracleDynamicParameters { BindByName = true };");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"Id\", userId, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return (await connection.QueryAsync<Claim>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                .ConfigureAwait(continueOnCapturedContext: false))
                            .AsList();
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateClaimBatchParameters(
                StringBuilder sb, string keyTypeName, IList<PropertyColumnTypeTriple> properties, IdentityDapperOptions options)
    {
        sb.AppendLine(
            """
                    protected override object CreateClaimBatchParameters(
                        IReadOnlyList<ApplicationUserClaim> claims, IReadOnlyList<string> parameterNames)
                    {
                        var parameters = new OracleDynamicParameters { BindByName = true };
                        for (var i = 0; i < claims.Count; i++)
                        {
                            var entity = claims[i];
                            var suffix = "_" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            foreach (var name in parameterNames)
                            {
                                switch (name)
                                {
            """);
        foreach (var property in properties)
        {
            var end = string.Equals(
                property.PropertyName,
                "UserId",
                StringComparison.Ordinal) ? $"{OracleTypeMapper.MapIdType(keyTypeName)}, ParameterDirection.Input, {OracleTypeMapper.MapIdSize(keyTypeName)});"
                : OracleTypeMapper.MapParameterEndByTypeName(property.PropertyType, options.StoreBooleanAs);
            sb.Append("                        case \"").Append(property.PropertyName).Append("\":\r\n                            parameters.Add(name + suffix, ").Append(PropertyAccess("entity", property.PropertyName)).Append(", ").Append(end).AppendLine("\r\n                            break;");
        }

        sb.AppendLine(
            """
                                    default:
                                        throw new InvalidOperationException("Unknown claim batch parameter.");
                                }
                            }
                        }
                        return parameters;
                    }

            """);
    }

    public static void GenerateAddClaimsImpl(
                StringBuilder sb,
                string keyTypeName,
                IList<PropertyColumnTypeTriple> properties,
                IdentityDapperOptions options)
    {
        sb.AppendLine(
            """
                    protected override async Task AddClaimsImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        IEnumerable<Claim> claims,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserClaimSql.CreateSql);
            """);

        sb.AppendLine("""
                        foreach (var claim in claims)
                        {
            """);
        sb.AppendLine(
            """
                            var entity = CreateUserClaim(user, claim);
                            var parameters = new OracleDynamicParameters { BindByName = true };
            """);
        GenerateEntityParameters(sb, properties, "entity", keyTypeName, options);

        sb.AppendLine(
            """
                            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("            }");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateReplaceClaimImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task ReplaceClaimImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        Claim claim,
                        Claim newClaim,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserClaimSql.ReplaceSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"UserId\", user.Id, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");\r\n            parameters.Add(\"ClaimTypeOld\", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);\r\n            parameters.Add(\"ClaimValueOld\", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);\r\n            parameters.Add(\"ClaimTypeNew\", newClaim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);\r\n            parameters.Add(\"ClaimValueNew\", newClaim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            """
                        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateRemoveClaimsImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task RemoveClaimsImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        IEnumerable<Claim> claims,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserClaimSql.DeleteSql);
                        foreach (var claim in claims)
                        {
                            var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("                parameters.Add(\"UserId\", user.Id, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");\r\n                parameters.Add(\"ClaimType\", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);\r\n                parameters.Add(\"ClaimValue\", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        sb.AppendLine(
            """
                            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("            }");

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateAddLoginImpl(
                StringBuilder sb,
                string keyTypeName,
                IList<PropertyColumnTypeTriple> properties,
                IdentityDapperOptions options)
    {
        sb.AppendLine(
            """
                    protected override async Task AddLoginImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        UserLoginInfo login,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserLoginSql.CreateSql);
                        var entity = CreateUserLogin(user, login);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        GenerateEntityParameters(sb, properties, "entity", keyTypeName, options);

        sb.AppendLine(
            """
                        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateRemoveLoginImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task RemoveLoginImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        string loginProvider,
                        string providerKey,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserLoginSql.DeleteSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"LoginProvider\", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);\r\n            parameters.Add(\"ProviderKey\", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);\r\n            parameters.Add(\"UserId\", user.Id, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                            .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateGetLoginsImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task<IList<UserLoginInfo>> GetLoginsImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserLoginSql.GetByUserIdSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"Id\", user.Id, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return (await connection.QueryAsync<ApplicationUserLogin>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                .ConfigureAwait(continueOnCapturedContext: false))
                            .Select(
                                login => new UserLoginInfo(
                                    login.LoginProvider,
                                    login.ProviderKey,
                                    login.ProviderDisplayName))
                            .AsList();
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindUserImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.Append("        protected override async Task<ApplicationUser?> FindUserImplAsync(\r\n            OracleConnection connection,\r\n            ").Append(keyTypeName).AppendLine(" userId,\r\n            CancellationToken cancellationToken)\r\n        {\r\n            var sql = NormalizeSql(IdentityUserSql.FindByIdSql);\r\n            var parameters = new OracleDynamicParameters { BindByName = true };");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"Id\", userId, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationUser>(sql, parameters, cancellationToken)
                                .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindUserLoginImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.Append("        protected override async Task<ApplicationUserLogin?> FindUserLoginImplAsync(\r\n            OracleConnection connection,\r\n            ").Append(keyTypeName).AppendLine(" userId,\r\n            string loginProvider,\r\n            string providerKey,\r\n            CancellationToken cancellationToken)\r\n        {\r\n            var sql = NormalizeSql(IdentityUserLoginSql.GetByUserIdLoginProviderKeySql);\r\n            var parameters = new OracleDynamicParameters { BindByName = true };");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"UserId\", userId, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");\r\n            parameters.Add(\"LoginProvider\", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);\r\n            parameters.Add(\"ProviderKey\", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);");

        sb.AppendLine(
            """
                        return await connection.QueryFirstOrDefaultAsync<ApplicationUserLogin>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindUserLoginImpl2(
                StringBuilder sb)
    {
        sb.AppendLine(
            """
                    protected override async Task<ApplicationUserLogin?> FindUserLoginImplAsync(
                        OracleConnection connection,
                        string loginProvider,
                        string providerKey,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserLoginSql.GetByLoginProviderKeySql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
                        parameters.Add("LoginProvider", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
                        parameters.Add("ProviderKey", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            """);

        sb.AppendLine(
            """
                        return await connection.QueryFirstOrDefaultAsync<ApplicationUserLogin>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindByEmailImpl(
                StringBuilder sb)
    {
        sb.AppendLine(
            """
                    protected override async Task<ApplicationUser?> FindByEmailImplAsync(
                        OracleConnection connection,
                        string normalizedEmail,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserSql.FindByEmailSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
                        parameters.Add("NormalizedEmail", normalizedEmail, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            """);

        sb.AppendLine(
            """
                        return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationUser>(sql, parameters, cancellationToken)
                                .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateGetUsersForClaimImpl(
                StringBuilder sb)
    {
        sb.AppendLine(
            """
                    protected override async Task<IList<ApplicationUser>> GetUsersForClaimImplAsync(
                        OracleConnection connection,
                        Claim claim,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserSql.GetUsersForClaimSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
                        parameters.Add("ClaimType", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                        parameters.Add("ClaimValue", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            """);

        sb.AppendLine(
            """
                        return (await connection.QueryIdentityAsync<ApplicationUser>(sql, parameters, cancellationToken)
                                    .ConfigureAwait(continueOnCapturedContext: false))
                                .AsList();
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateFindTokenImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task<ApplicationUserToken?> FindTokenImplAsync(
                        OracleConnection connection,
                        ApplicationUser user,
                        string loginProvider,
                        string name,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserTokenSql.GetByUserIdSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"UserId\", user.Id, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");\r\n            parameters.Add(\"LoginProvider\", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);\r\n            parameters.Add(\"Name\", name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);");

        sb.AppendLine(
            """
                        return await connection.QueryFirstOrDefaultAsync<ApplicationUserToken>(
                                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                 .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateAddUserTokenImpl(
                StringBuilder sb,
                string keyTypeName,
                IList<PropertyColumnTypeTriple> properties,
                IdentityDapperOptions options)
    {
        sb.AppendLine(
            """
                    protected override async Task AddUserTokenImplAsync(
                        OracleConnection connection,
                        ApplicationUserToken token,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserTokenSql.CreateSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        GenerateEntityParameters(sb, properties, "token", keyTypeName, options);

        sb.AppendLine(
            """
                        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                 .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateTryUpdateTokenImpl(StringBuilder sb, string keyTypeName)
    {
        sb.Append("        protected override async Task<bool> TryUpdateTokenImplAsync(\r\n            OracleConnection connection,\r\n            ApplicationUserToken token,\r\n            string? originalValue,\r\n            CancellationToken cancellationToken)\r\n        {\r\n            if (IdentityUserTokenSql is not IIdentityUserTokenConcurrencySql sql)\r\n            {\r\n                throw new NotSupportedException(\"Regenerate the Identity stores to enable atomic token updates.\");\r\n            }\r\n\r\n            var parameters = new OracleDynamicParameters { BindByName = true };\r\n            parameters.Add(\"UserId\", token.UserId, ").Append(OracleTypeMapper.MapIdType(keyTypeName)).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(OracleTypeMapper.MapIdSize(keyTypeName)).AppendLine(");\r\n            parameters.Add(\"LoginProvider\", token.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);\r\n            parameters.Add(\"Name\", token.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);\r\n            parameters.Add(\"Value\", token.Value, OracleMappingType.Varchar2, ParameterDirection.Input);\r\n            parameters.Add(\"OriginalValue\", originalValue, OracleMappingType.Varchar2, ParameterDirection.Input);\r\n            return await connection.ExecuteAsync(new CommandDefinition(\r\n                    NormalizeSql(sql.UpdateSql), parameters, cancellationToken: cancellationToken))\r\n                .ConfigureAwait(continueOnCapturedContext: false) == 1;\r\n        }");
        sb.AppendLine();
    }

    public static void GenerateRemoveUserTokenImpl(
                StringBuilder sb,
                string keyTypeName)
    {
        sb.AppendLine(
            """
                    protected override async Task RemoveUserTokenImplAsync(
                        OracleConnection connection,
                        ApplicationUserToken token,
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(IdentityUserTokenSql.DeleteSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"LoginProvider\", token.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);\r\n            parameters.Add(\"Name\", token.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);\r\n            parameters.Add(\"UserId\", token.UserId, ").Append(idType).Append(OracleStoreMethodGenerator.InputParameterDirectionArgument).Append(idSize).AppendLine(");");

        sb.AppendLine(
            """
                        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                                 .ConfigureAwait(continueOnCapturedContext: false);
            """);

        sb.AppendLine("        }");

        sb.AppendLine();
    }

    public static void GenerateEntityParameters(
                StringBuilder sb,
                IList<PropertyColumnTypeTriple> properties,
                string entity,
                string keyTypeName,
                IdentityDapperOptions options)
    {
        foreach (var property in properties)
        {
            var end = property.PropertyName is "UserId" or "RoleId"
                ? $"{OracleTypeMapper.MapIdType(keyTypeName)}, ParameterDirection.Input, {OracleTypeMapper.MapIdSize(keyTypeName)});"
                : OracleTypeMapper.MapParameterEndByTypeName(property.PropertyType, options.StoreBooleanAs);
            sb.Append("                parameters.Add(\"").Append(property.PropertyName).Append("\", ").Append(PropertyAccess(entity, property.PropertyName)).Append(", ").AppendLine(end);
        }
    }

    internal static string PropertyAccess(string entity, string propertyName) =>
                SyntaxFacts.GetKeywordKind(propertyName) != SyntaxKind.None ||
                SyntaxFacts.GetContextualKeywordKind(propertyName) != SyntaxKind.None
                    ? $"{entity}.@{propertyName}"
                    : $"{entity}.{propertyName}";

    private static void GenerateUserParameters(StringBuilder sb, IdentityDapperOptions options, string concurrencyStamp)
    {
        sb.AppendLine(
            """            parameters.Add("UserName", user.UserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);""");

        if (!options.SkipNormalized)
        {
            sb.AppendLine(
                """            parameters.Add("NormalizedUserName", user.NormalizedUserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);""");
        }

        sb.AppendLine(
            """            parameters.Add("Email", user.Email, OracleMappingType.Varchar2, ParameterDirection.Input, 256);""");

        if (!options.SkipNormalized)
        {
            sb.AppendLine(
                """            parameters.Add("NormalizedEmail", user.NormalizedEmail, OracleMappingType.Varchar2, ParameterDirection.Input, 256);""");
        }

        GenerateBooleanParameter(sb, options, nameof(IdentityUser<int>.EmailConfirmed));

        sb.Append("            parameters.Add(\"PasswordHash\", user.PasswordHash, OracleMappingType.Varchar2, ParameterDirection.Input, 256);\r\n            parameters.Add(\"SecurityStamp\", user.SecurityStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);\r\n            parameters.Add(\"ConcurrencyStamp\", ").Append(concurrencyStamp).AppendLine(", OracleMappingType.Varchar2, ParameterDirection.Input, 256);\r\n            parameters.Add(\"PhoneNumber\", user.PhoneNumber, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");

        GenerateBooleanParameter(sb, options, nameof(IdentityUser<int>.PhoneNumberConfirmed));
        GenerateBooleanParameter(sb, options, nameof(IdentityUser<int>.TwoFactorEnabled));

        sb.AppendLine(
            """            parameters.Add("LockoutEnd", user.LockoutEnd, OracleMappingType.TimeStamp, ParameterDirection.Input);""");

        GenerateBooleanParameter(sb, options, nameof(IdentityUser<int>.LockoutEnabled));

        sb.AppendLine(
            """            parameters.Add("AccessFailedCount", user.AccessFailedCount, OracleMappingType.Int32, ParameterDirection.Input);""");
    }

    private static void GenerateBooleanParameter(StringBuilder sb, IdentityDapperOptions options, string property)
    {
        if (string.Equals(options.StoreBooleanAs, "char", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "number", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "numeric", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "string", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("            parameters.Add(\"").Append(property).Append("\", user.").Append(property).Append(", ").AppendLine(OracleTypeMapper.MapParameterEndByStoreBooleanAs(options.StoreBooleanAs));
        }
    }
}
