using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper;

internal sealed class IdentityClaimBatchParameters<TClaim> : SqlMapper.IDynamicParameters
{
    private readonly IReadOnlyList<TClaim> _claims;
    private readonly HashSet<string> _names;

    internal IdentityClaimBatchParameters(IReadOnlyList<TClaim> claims, IReadOnlyList<string> names)
    {
        _claims = claims;
        _names = new HashSet<string>(names, StringComparer.Ordinal);
    }

    public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
    {
        for (var i = 0; i < _claims.Count; i++)
        {
            // Reuse Dapper's typed binders, including nullable properties and registered handlers.
            // Batch SQL exposes unsuffixed names in a comment for Dapper's typed property filter.
            using var source = command.Connection!.CreateCommand();
#pragma warning disable SCS0002
            source.CommandText = command.CommandText;
#pragma warning restore SCS0002
            var parameters = new DynamicParameters(_claims[i]);
            ((SqlMapper.IDynamicParameters)parameters).AddParameters(source, identity);
            var suffix = "_" + i.ToString(CultureInfo.InvariantCulture);
            while (source.Parameters.Count != 0)
            {
                var parameter = (IDbDataParameter)source.Parameters[0]!;
                source.Parameters.RemoveAt(0);
                if (_names.Contains(parameter.ParameterName))
                {
                    // Rename before adding: provider name-index caches must never see unsuffixed names.
                    parameter.ParameterName += suffix;
                    command.Parameters.Add(parameter);
                }
            }
        }
    }
}
