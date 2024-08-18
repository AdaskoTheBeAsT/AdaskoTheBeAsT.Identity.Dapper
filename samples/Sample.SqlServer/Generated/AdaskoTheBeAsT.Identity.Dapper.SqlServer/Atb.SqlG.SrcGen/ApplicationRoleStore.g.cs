using System;
using AdaskoTheBeAsT.Identity.Dapper;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace Sample.SqlServer
{
    public class ApplicationRoleStore
        : DapperRoleStoreBase<ApplicationRole, Guid, ApplicationRoleClaim, SqlConnection>
    {
        public ApplicationRoleStore(
            IIdentityDbConnectionProvider<SqlConnection> connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityRoleSql(),
                new IdentityRoleClaimSql())
        {
        }
    }
}
