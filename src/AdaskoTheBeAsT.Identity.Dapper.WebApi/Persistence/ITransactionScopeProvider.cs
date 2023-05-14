using System.Transactions;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Persistence;

public interface ITransactionScopeProvider
{
    TransactionScope Provide();
}
