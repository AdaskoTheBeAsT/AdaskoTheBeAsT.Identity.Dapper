﻿//HintName: ApplicationUserStore.g.cs
using System;
using AdaskoTheBeAsT.Identity.Dapper;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample
{
    public class ApplicationUserStore
        : DapperUserStoreBase<ApplicationUser, ApplicationRole, Guid, ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin, ApplicationUserToken, SqlConnection>
    {
        public ApplicationUserStore(
            IIdentityDbConnectionProvider<SqlConnection> connectionProvider)
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
