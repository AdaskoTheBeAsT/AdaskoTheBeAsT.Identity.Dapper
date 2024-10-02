//HintName: ApplicationUserStore.g.cs
using System;
using AdaskoTheBeAsT.Identity.Dapper;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class ApplicationUserStore
        : DapperUserStoreBase<ApplicationUser, ApplicationRole, Guid, ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin, ApplicationUserToken, NpgsqlConnection>
    {
        public ApplicationUserStore(
            IIdentityDbConnectionProvider<NpgsqlConnection> connectionProvider)
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
    }
}
