//HintName: ApplicationUserStore.g.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Dapper;
using Dapper.Oracle;
using Microsoft.AspNetCore.Identity;
using Oracle.ManagedDataAccess.Client;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class ApplicationUserStore
        : DapperUserStoreBase<ApplicationUser, ApplicationRole, Guid, ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin, ApplicationUserToken, OracleConnection>
    {
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

        public override IQueryable<ApplicationUser> Users
        {
            get
            {
                ThrowIfDisposed();
                using var connection = ConnectionProvider.Provide();
                return connection.QueryIdentity<ApplicationUser>(NormalizeSql(IdentityUserSql.GetUsersSql)).AsQueryable();
            }
        }

        private static string NormalizeSql(string sql)
        {
            return sql.StartsWith("DECLARE", StringComparison.OrdinalIgnoreCase) ||
                   sql.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase)
                ? sql
                : sql.Trim().TrimEnd(';');
        }

        protected override async Task CreateImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserSql.CreateSql;
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("OutputId", dbType: OracleMappingType.Raw, direction: ParameterDirection.ReturnValue, size: 16);
            parameters.Add("Id", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("UserName", user.UserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("NormalizedUserName", user.NormalizedUserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("Email", user.Email, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("NormalizedEmail", user.NormalizedEmail, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("EmailConfirmed", user.EmailConfirmed, OracleMappingType.Char, ParameterDirection.Input, 1);
            parameters.Add("PasswordHash", user.PasswordHash, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("SecurityStamp", user.SecurityStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ConcurrencyStamp", user.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("PhoneNumber", user.PhoneNumber, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("PhoneNumberConfirmed", user.PhoneNumberConfirmed, OracleMappingType.Char, ParameterDirection.Input, 1);
            parameters.Add("TwoFactorEnabled", user.TwoFactorEnabled, OracleMappingType.Char, ParameterDirection.Input, 1);
            parameters.Add("LockoutEnd", user.LockoutEnd, OracleMappingType.TimeStamp, ParameterDirection.Input);
            parameters.Add("LockoutEnabled", user.LockoutEnabled, OracleMappingType.Char, ParameterDirection.Input, 1);
            parameters.Add("AccessFailedCount", user.AccessFailedCount, OracleMappingType.Int32, ParameterDirection.Input);
            parameters.Add("Active", user.Active, OracleMappingType.Char, ParameterDirection.Input, 1);
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
            var idBytes = parameters.Get<byte[]>("OutputId");
            user.Id = new Guid(idBytes);
        }

        protected override async Task UpdateImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserSql.UpdateSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            var stamp = Guid.NewGuid().ToString();
            parameters.Add("OriginalConcurrencyStamp", user.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("Id", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("UserName", user.UserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("NormalizedUserName", user.NormalizedUserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("Email", user.Email, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("NormalizedEmail", user.NormalizedEmail, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("EmailConfirmed", user.EmailConfirmed, OracleMappingType.Char, ParameterDirection.Input, 1);
            parameters.Add("PasswordHash", user.PasswordHash, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("SecurityStamp", user.SecurityStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ConcurrencyStamp", stamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("PhoneNumber", user.PhoneNumber, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("PhoneNumberConfirmed", user.PhoneNumberConfirmed, OracleMappingType.Char, ParameterDirection.Input, 1);
            parameters.Add("TwoFactorEnabled", user.TwoFactorEnabled, OracleMappingType.Char, ParameterDirection.Input, 1);
            parameters.Add("LockoutEnd", user.LockoutEnd, OracleMappingType.TimeStamp, ParameterDirection.Input);
            parameters.Add("LockoutEnabled", user.LockoutEnabled, OracleMappingType.Char, ParameterDirection.Input, 1);
            parameters.Add("AccessFailedCount", user.AccessFailedCount, OracleMappingType.Int32, ParameterDirection.Input);
            parameters.Add("Active", user.Active, OracleMappingType.Char, ParameterDirection.Input, 1);
            var affected = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
            if (affected != 1)
            {
                throw new DBConcurrencyException();
            }

            user.ConcurrencyStamp = stamp;
        }

        protected override async Task DeleteImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserSql.DeleteSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("ConcurrencyStamp", user.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("Id", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            var affected = await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
            if (affected != 1)
            {
                throw new DBConcurrencyException();
            }
        }

        protected override async Task<ApplicationUser?> FindByIdImplAsync(
            OracleConnection connection,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserSql.FindByIdSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("Id", userId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationUser>(sql, parameters, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationUser?> FindByNameImplAsync(
            OracleConnection connection,
            string normalizedUserName,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserSql.FindByNameSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("NormalizedUserName", normalizedUserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationUser>(sql, parameters, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<IList<Claim>> GetClaimsImplAsync(
            OracleConnection connection,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserClaimSql.GetByUserIdSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("Id", userId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return (await connection.QueryAsync<Claim>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();
        }

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
                        case "UserId":
                            parameters.Add(name + suffix, entity.UserId, OracleMappingType.Raw, ParameterDirection.Input, 16);
                            break;
                        case "ClaimType":
                            parameters.Add(name + suffix, entity.ClaimType, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                            break;
                        case "ClaimValue":
                            parameters.Add(name + suffix, entity.ClaimValue, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                            break;
                        default:
                            throw new InvalidOperationException("Unknown claim batch parameter.");
                    }
                }
            }
            return parameters;
        }

        protected override async Task ReplaceClaimImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            Claim claim,
            Claim newClaim,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserClaimSql.ReplaceSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("ClaimTypeOld", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ClaimValueOld", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ClaimTypeNew", newClaim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ClaimValueNew", newClaim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task AddLoginImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            UserLoginInfo login,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserLoginSql.CreateSql);
            var entity = CreateUserLogin(user, login);
            var parameters = new OracleDynamicParameters { BindByName = true };
                parameters.Add("LoginProvider", entity.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                parameters.Add("ProviderKey", entity.ProviderKey, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                parameters.Add("ProviderDisplayName", entity.ProviderDisplayName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                parameters.Add("UserId", entity.UserId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task RemoveLoginImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            string loginProvider,
            string providerKey,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserLoginSql.DeleteSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("LoginProvider", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("ProviderKey", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<IList<UserLoginInfo>> GetLoginsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserLoginSql.GetByUserIdSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("Id", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return (await connection.QueryAsync<ApplicationUserLogin>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                    .ConfigureAwait(continueOnCapturedContext: false))
                .Select(
                    login => new UserLoginInfo(
                        login.LoginProvider,
                        login.ProviderKey,
                        login.ProviderDisplayName))
                .AsList();
        }

        protected override async Task<ApplicationUser?> FindUserImplAsync(
            OracleConnection connection,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserSql.FindByIdSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("Id", userId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationUser>(sql, parameters, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationUserLogin?> FindUserLoginImplAsync(
            OracleConnection connection,
            Guid userId,
            string loginProvider,
            string providerKey,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserLoginSql.GetByUserIdLoginProviderKeySql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("UserId", userId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("LoginProvider", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("ProviderKey", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            return await connection.QueryFirstOrDefaultAsync<ApplicationUserLogin>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

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
            return await connection.QueryFirstOrDefaultAsync<ApplicationUserLogin>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationUser?> FindByEmailImplAsync(
            OracleConnection connection,
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserSql.FindByEmailSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("NormalizedEmail", normalizedEmail, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationUser>(sql, parameters, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<IList<ApplicationUser>> GetUsersForClaimImplAsync(
            OracleConnection connection,
            Claim claim,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserSql.GetUsersForClaimSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("ClaimType", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ClaimValue", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            return (await connection.QueryIdentityAsync<ApplicationUser>(sql, parameters, cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false))
                    .AsList();
        }

        protected override async Task<ApplicationUserToken?> FindTokenImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            string loginProvider,
            string name,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserTokenSql.GetByUserIdSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("LoginProvider", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("Name", name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            return await connection.QueryFirstOrDefaultAsync<ApplicationUserToken>(
                    new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                     .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task AddUserTokenImplAsync(
            OracleConnection connection,
            ApplicationUserToken token,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserTokenSql.CreateSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
                parameters.Add("UserId", token.UserId, OracleMappingType.Raw, ParameterDirection.Input, 16);
                parameters.Add("LoginProvider", token.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                parameters.Add("Name", token.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                parameters.Add("Value", token.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                     .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<bool> TryUpdateTokenImplAsync(
            OracleConnection connection,
            ApplicationUserToken token,
            string? originalValue,
            CancellationToken cancellationToken)
        {
            if (IdentityUserTokenSql is not IIdentityUserTokenConcurrencySql sql)
            {
                throw new NotSupportedException("Regenerate the Identity stores to enable atomic token updates.");
            }

            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("UserId", token.UserId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("LoginProvider", token.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("Name", token.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("Value", token.Value, OracleMappingType.Varchar2, ParameterDirection.Input);
            parameters.Add("OriginalValue", originalValue, OracleMappingType.Varchar2, ParameterDirection.Input);
            return await connection.ExecuteAsync(new CommandDefinition(
                    NormalizeSql(sql.UpdateSql), parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(continueOnCapturedContext: false) == 1;
        }

        protected override async Task RemoveUserTokenImplAsync(
            OracleConnection connection,
            ApplicationUserToken token,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserTokenSql.DeleteSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("LoginProvider", token.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("Name", token.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("UserId", token.UserId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                     .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<IList<ApplicationUser>> GetUsersInRoleImplAsync(
            OracleConnection connection,
            string roleName,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserSql.GetUsersInRoleSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("NormalizedName", roleName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            return (await connection.QueryIdentityAsync<ApplicationUser>(sql, parameters, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();
        }

        protected override async Task AddToRoleImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserRoleSql.CreateSql);
            var entity = CreateUserRole(user, role);
            var parameters = new OracleDynamicParameters { BindByName = true };
                parameters.Add("UserId", entity.UserId, OracleMappingType.Raw, ParameterDirection.Input, 16);
                parameters.Add("RoleId", entity.RoleId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task RemoveFromRoleImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserRoleSql.DeleteSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("RoleId", role.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<IList<string>> GetRolesImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserRoleSql.GetRoleNamesByUserIdSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return (await connection.QueryAsync<string>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();
        }

        protected override async Task<bool> IsInRoleImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserRoleSql.GetCountSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("RoleId", role.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return (await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                    .ConfigureAwait(continueOnCapturedContext: false)) > 0;
        }

        protected override async Task<IList<Claim>> GetRoleClaimsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserRoleClaimSql.GetRoleClaimsByUserIdSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("Id", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return (await connection.QueryAsync<Claim>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                        .ConfigureAwait(continueOnCapturedContext: false))
                    .AsList();
        }

        protected override async Task<IList<Claim>> GetUserAndRoleClaimsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserRoleClaimSql.GetUserAndRoleClaimsByUserIdSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("Id", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return (await connection.QueryAsync<Claim>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                        .ConfigureAwait(continueOnCapturedContext: false))
                    .AsList();
        }

        protected override async Task<ApplicationRole?> FindRoleImplAsync(
            OracleConnection connection,
            string roleName,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityRoleSql.FindByNameSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("NormalizedName", roleName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            return await connection.QueryIdentityFirstOrDefaultAsync<ApplicationRole>(sql, parameters, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationUserRole?> FindUserRoleAsync(
            OracleConnection connection,
            Guid userId,
            Guid roleId,
            CancellationToken cancellationToken)
        {
            var sql = NormalizeSql(IdentityUserRoleSql.GetByUserIdRoleIdSql);
            var parameters = new OracleDynamicParameters { BindByName = true };
            parameters.Add("UserId", userId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("RoleId", roleId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return await connection.QueryFirstOrDefaultAsync<ApplicationUserRole>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

    }
}
