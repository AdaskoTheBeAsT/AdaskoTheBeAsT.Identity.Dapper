//HintName: ApplicationRoleStore.g.cs
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
    public class ApplicationRoleStore
        : DapperRoleStoreBase<ApplicationRole, Guid, ApplicationRoleClaim, OracleConnection>
    {
        public ApplicationRoleStore(
            IIdentityDbConnectionProvider<OracleConnection> connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityRoleSql(),
                new IdentityRoleClaimSql())
        {
        }

        protected override async Task CreateImplAsync(
            OracleConnection connection,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {
            var sql = IdentityRoleSql.CreateSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("OutputId", dbType: OracleMappingType.Raw, direction: ParameterDirection.ReturnValue, size: 16);
            parameters.Add("Name", role.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("NormalizedName", role.NormalizedName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ConcurrencyStamp", role.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);
            var idBytes = parameters.Get<byte[]>("OutputId");
            role.Id = new Guid(idBytes);
        }

        protected override async Task UpdateImplAsync(
            OracleConnection connection,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {
            var sql = IdentityRoleSql.UpdateSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", role.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("Name", role.Name, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("NormalizedName", role.NormalizedName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ConcurrencyStamp", role.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task DeleteImplAsync(
            OracleConnection connection,
            ApplicationRole role,
            CancellationToken cancellationToken)
        {
            var sql = IdentityRoleSql.DeleteSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", role.Id, OracleMappingType.Raw, ParameterDirection.Input, 16);
            await connection.ExecuteAsync(sql, parameters).ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationRole?> FindByIdImplAsync(
            OracleConnection connection,
            Guid roleId,
            CancellationToken cancellationToken)
        {
            var sql = IdentityRoleSql.FindByIdSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", roleId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return await connection.QueryFirstOrDefaultAsync<ApplicationRole>(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<ApplicationRole?> FindByNameImplAsync(
            OracleConnection connection,
            string normalizedRoleName,
            CancellationToken cancellationToken)
        {
            var sql = IdentityRoleSql.FindByNameSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("NormalizedName", normalizedRoleName, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            return await connection.QueryFirstOrDefaultAsync<ApplicationRole>(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task<IList<Claim>> GetClaimsImplAsync(
            OracleConnection connection,
            Guid roleId,
            CancellationToken cancellationToken)
        {
            var sql = IdentityRoleClaimSql.GetByRoleIdSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("Id", roleId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            return (await connection.QueryAsync<Claim>(sql, parameters)
                    .ConfigureAwait(continueOnCapturedContext: false))
                .AsList();
        }

        protected override async Task AddClaimImplAsync(
            OracleConnection connection,
            ApplicationRoleClaim roleClaim,
            CancellationToken cancellationToken)
        {
            var sql = IdentityRoleClaimSql.CreateSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("RoleId", roleClaim.RoleId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("ClaimType", roleClaim.ClaimType, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ClaimValue", roleClaim.ClaimValue, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        protected override async Task RemoveClaimImplAsync(
            OracleConnection connection,
            ApplicationRoleClaim roleClaim,
            CancellationToken cancellationToken)
        {
            var sql = IdentityRoleClaimSql.DeleteSql;
            var parameters = new OracleDynamicParameters();
            parameters.Add("RoleId", roleClaim.RoleId, OracleMappingType.Raw, ParameterDirection.Input, 16);
            parameters.Add("ClaimType", roleClaim.ClaimType, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            parameters.Add("ClaimValue", roleClaim.ClaimValue, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            await connection.ExecuteAsync(sql, parameters)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
