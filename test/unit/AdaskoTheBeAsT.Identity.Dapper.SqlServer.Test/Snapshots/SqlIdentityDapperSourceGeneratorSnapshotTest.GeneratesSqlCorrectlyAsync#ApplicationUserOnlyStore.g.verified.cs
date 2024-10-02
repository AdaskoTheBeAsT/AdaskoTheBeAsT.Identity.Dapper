//HintName: ApplicationUserOnlyStore.g.cs
using System;
using AdaskoTheBeAsT.Identity.Dapper;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class ApplicationUserOnlyStore
        : DapperUserOnlyStoreBase<ApplicationUser, Guid, ApplicationUserClaim, ApplicationUserLogin, ApplicationUserToken, SqlConnection>
    {
        public ApplicationUserOnlyStore(
            IIdentityDbConnectionProvider<SqlConnection> connectionProvider)
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
