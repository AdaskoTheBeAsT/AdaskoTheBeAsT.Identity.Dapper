using System.Data;

namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityDbConnectionProvider
{
    IDbConnection Provide();
}
