using System;
using AdaskoTheBeAsT.Identity.Dapper;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Sample.SqlServer
{
    public class ApplicationRoleStore
        : DapperRoleStoreBase<ApplicationRole, Guid, ApplicationRoleClaim>
    {
        public ApplicationRoleStore(
            IIdentityDbConnectionProvider connectionProvider)
            : base(
                new IdentityErrorDescriber(),
                connectionProvider,
                new IdentityRoleSql(),
                new IdentityRoleClaimSql())
        {
        }
    }
}
