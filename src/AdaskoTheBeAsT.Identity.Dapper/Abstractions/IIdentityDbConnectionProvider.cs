using System.Data;

namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityDbConnectionProvider<out TDbConnection>
    where TDbConnection : IDbConnection
{
    TDbConnection Provide();
}
