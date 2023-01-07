using System.Data.Common;

namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

public interface IIdentityDbConnectionProvider
{
    DbConnection Provide();
}
