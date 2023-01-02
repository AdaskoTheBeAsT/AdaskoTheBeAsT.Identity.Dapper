using System.Data.Common;

namespace AdaskoTheBeAsT.Identity.Dapper;

public interface IIdentityDbConnectionProvider
{
    DbConnection Provide();
}
