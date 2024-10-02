//HintName: ApplicationUserOnlyStore.g.cs
#nullable enable
using System;
using System.Data;
using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Dapper;
using Dapper.Oracle;
using Microsoft.AspNetCore.Identity;
using Oracle.ManagedDataAccess.Client;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class ApplicationUserOnlyStore
        : DapperUserOnlyStoreBase<ApplicationUser, Guid, ApplicationUserClaim, ApplicationUserLogin, ApplicationUserToken, OracleConnection>
    {
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

        protected override async Task CreateImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserSql.CreateSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("OutputId", dbType: OracleMappingType.Raw, direction: ParameterDirection.ReturnValue, size: 16);
            parameters.Add("UserName", user.UserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("NormalizedUserName", user.NormalizedUserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("Email", user.Email, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("NormalizedEmail", user.NormalizedEmail, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("PasswordHash", user.PasswordHash, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("SecurityStamp", user.SecurityStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ConcurrencyStamp", user.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("PhoneNumber", user.PhoneNumber, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("LockoutEnd", user.LockoutEnd, OracleMappingType.TimeStamp, ParameterDirection.Input);
            parameters.Add("AccessFailedCount", user.AccessFailedCount, OracleMappingType.Int32, ParameterDirection.Input);
            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);
            var idBytes = parameters.Get<byte[]>("OutputId");
            user.Id = new Guid(idBytes);
        }

        protected override async Task UpdateImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserSql.UpdateSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("UserName", user.UserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("NormalizedUserName", user.NormalizedUserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("Email", user.Email, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("NormalizedEmail", user.NormalizedEmail, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("PasswordHash", user.PasswordHash, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("SecurityStamp", user.SecurityStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ConcurrencyStamp", user.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("PhoneNumber", user.PhoneNumber, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("LockoutEnd", user.LockoutEnd, OracleMappingType.TimeStamp, ParameterDirection.Input);
            parameters.Add("AccessFailedCount", user.AccessFailedCount, OracleMappingType.Int32, ParameterDirection.Input);
            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task DeleteImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserSql.DeleteSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationUser?> FindByIdImplAsync(
            OracleConnection connection,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserSql.FindByIdSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", userId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationUser?> FindByNameImplAsync(
            OracleConnection connection,
            string normalizedUserName,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserSql.FindByNameSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("NormalizedName", normalizedUserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            return await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<IList<Claim>> GetClaimsImplAsync(
            OracleConnection connection,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserClaimSql.GetByUserIdSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", userId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return (await connection.QueryAsync<Claim>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();
        }

        protected override async Task AddClaimsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            IEnumerable<Claim> claims,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserClaimSql.CreateSql;
            foreach (var claim in claims)
            {
                var parameters = new OracleDynamicParameters();
                parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
                parameters.Add("ClaimType", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                parameters.Add("ClaimValue", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                await connection.ExecuteAsync(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        protected override async Task ReplaceClaimImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            Claim claim,
            Claim newClaim,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserClaimSql.ReplaceSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("ClaimTypeOld", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ClaimValueOld", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ClaimTypeNew", newClaim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ClaimValueNew", newClaim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task RemoveClaimsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            IEnumerable<Claim> claims,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserClaimSql.DeleteSql;
            foreach (var claim in claims)
            {
                var parameters = new OracleDynamicParameters();
                parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
                parameters.Add("ClaimType", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                parameters.Add("ClaimValue", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
                await connection.ExecuteAsync(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        protected override async Task AddLoginImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            UserLoginInfo login,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserLoginSql.CreateSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("LoginProvider", login.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("ProviderKey", login.ProviderKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("ProviderDisplayName", login.ProviderDisplayName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task RemoveLoginImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            string loginProvider,
            string providerKey,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserLoginSql.DeleteSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("LoginProvider", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("ProviderKey", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<IList<UserLoginInfo>> GetLoginsImplAsync(
            OracleConnection connection,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserLoginSql.GetByUserIdSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return (await connection.QueryAsync<UserLoginInfo>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();
        }

        protected override async Task<ApplicationUser?> FindUserImplAsync(
            OracleConnection connection,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserSql.FindByIdSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", userId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationUserLogin?> FindUserLoginImplAsync(
            OracleConnection connection,
            Guid userId,
            string loginProvider,
            string providerKey,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserLoginSql.GetByUserIdLoginProviderKeySql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", userId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("LoginProvider", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("ProviderKey", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            return await connection.QueryFirstOrDefaultAsync<ApplicationUserLogin>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationUserLogin?> FindUserLoginImplAsync(
            OracleConnection connection,
            string loginProvider,
            string providerKey,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserLoginSql.GetByLoginProviderKeySql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("LoginProvider", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("ProviderKey", providerKey, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            return await connection.QueryFirstOrDefaultAsync<ApplicationUserLogin>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationUser?> FindByEmailImplAsync(
            OracleConnection connection,
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserSql.FindByEmailSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("NormalizedEmail", normalizedEmail, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            return await connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<IList<ApplicationUser>> GetUsersForClaimImplAsync(
            OracleConnection connection,
            Claim claim,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserSql.GetUsersForClaimSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("ClaimType", claim.Type, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ClaimValue", claim.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            return (await connection.QueryAsync<ApplicationUser>(sql, parameters)
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
            var sql = IdentityUserTokenSql.GetByUserIdSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("UserId", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("LoginProvider", loginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("Name", name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            return await connection.QueryFirstOrDefaultAsync<ApplicationUserToken>(sql, parameters)
                     .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task AddUserTokenImplAsync(
            OracleConnection connection,
            ApplicationUserToken token,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserTokenSql.CreateSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("UserId", token.UserId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("LoginProvider", token.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("Name", token.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("Value", token.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            await connection.ExecuteAsync(sql, parameters)
                     .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task RemoveUserTokenImplAsync(
            OracleConnection connection,
            ApplicationUserToken token,
            CancellationToken cancellationToken)
        {
            var sql = IdentityUserTokenSql.DeleteSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("LoginProvider", token.LoginProvider, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("Name", token.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("Value", token.Value, OracleMappingType.Varchar2, ParameterDirection.Input, 128);
            parameters.Add("UserId", token.UserId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            await connection.ExecuteAsync(sql, parameters)
                     .ConfigureAwait(continueOnCapturedContext: false);
        }

    }
}
