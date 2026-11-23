using Microsoft.Data.SqlClient;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Internal;

public interface ISqlServerJwtConnectionFactory
{
    SqlConnection CreateConnection();
}
