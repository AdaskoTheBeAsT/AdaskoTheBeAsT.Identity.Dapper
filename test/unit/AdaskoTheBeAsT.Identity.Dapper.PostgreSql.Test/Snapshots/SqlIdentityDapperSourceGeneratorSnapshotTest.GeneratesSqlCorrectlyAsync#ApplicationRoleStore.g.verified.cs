//HintName: ApplicationRoleStore.g.cs
using System;
using AdaskoTheBeAsT.Identity.Dapper;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class ApplicationRoleStore
        : DapperRoleStoreBase<ApplicationRole, Guid, ApplicationRoleClaim, NpgsqlConnection>
    {
        public ApplicationRoleStore(
            IIdentityDbConnectionProvider<NpgsqlConnection> connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityRoleSql(),
                new IdentityRoleClaimSql())
        {
        }
    }
}
