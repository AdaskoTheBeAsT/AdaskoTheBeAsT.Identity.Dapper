using System;
using System.Data;
using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Dapper;
using Dapper.Oracle;
using Microsoft.AspNetCore.Identity;
using Oracle.ManagedDataAccess.Client;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.Identity
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
            parameters.Add("Id", user.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("UserName", user.UserName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("Email", user.Email, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
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
            parameters.Add("Email", user.Email, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
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
    }
}
