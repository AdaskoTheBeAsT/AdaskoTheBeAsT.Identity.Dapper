//HintName: ApplicationUserOnlyStore.g.cs
using System;
using AdaskoTheBeAsT.Identity.Dapper;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class ApplicationUserOnlyStore
        : DapperUserOnlyStoreBase<ApplicationUser, Guid, ApplicationUserClaim, ApplicationUserLogin, ApplicationUserToken, NpgsqlConnection>
    {
        public ApplicationUserOnlyStore(
            IIdentityDbConnectionProvider<NpgsqlConnection> connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityUserSql(),
                new IdentityUserClaimSql(),
                new IdentityUserLoginSql(),
                new IdentityUserTokenSql())
        {
        }
    }
}
