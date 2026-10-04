using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.Abstractions;

/// <summary>Composite-format per-claim statements using {0} for the bind-name index.</summary>
public interface IIdentityUserClaimBatchSql : IIdentityUserClaimSql
{
    string CreateBatchItemSql { get; }

    string DeleteBatchItemSql { get; }

    IReadOnlyList<string> CreateBatchParameterNames { get; }

    string BatchPrefix { get; }

    string BatchSuffix { get; }
}
