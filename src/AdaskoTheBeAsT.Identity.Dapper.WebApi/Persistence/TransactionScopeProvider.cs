using System.Transactions;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Persistence;

public class TransactionScopeProvider
    : ITransactionScopeProvider
{
    public TransactionScope Provide()
    {
        var options = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TransactionManager.DefaultTimeout,
        };

        return new TransactionScope(
            TransactionScopeOption.Required,
            options,
            TransactionScopeAsyncFlowOption.Enabled);
    }
}
