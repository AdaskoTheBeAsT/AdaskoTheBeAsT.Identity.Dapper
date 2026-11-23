using System.Data.Common;
using System.Globalization;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal sealed class RelationalSession(
    DbConnection connection,
    DbTransaction? transaction,
    SqlDialect dialect,
    CancellationToken cancellationToken)
{
    public async Task<int> ExecuteAsync(string sql, params object?[] args)
    {
        var command = CreateCommand(sql, args);
        await using (command.ConfigureAwait(false))
        {
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<List<T>> QueryAsync<T>(string sql, Func<DbDataReader, T> map, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(map);
        var command = CreateCommand(sql, args);
        await using (command.ConfigureAwait(false))
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                var results = new List<T>();
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(map(reader));
                }

                return results;
            }
        }
    }

    private DbCommand CreateCommand(string sql, object?[] args)
    {
        var command = connection.CreateCommand();
        try
        {
            command.Transaction = transaction;
#pragma warning disable CA2100, SCS0002 // Identifiers are validated configuration; record values are bound parameters.
            command.CommandText = dialect.IsOracle ? sql.Replace('@', ':') : sql;
#pragma warning restore CA2100, SCS0002
            dialect.ConfigureCommand(command);
            for (var i = 0; i < args.Length; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "p" + i.ToString(CultureInfo.InvariantCulture);
                if (args[i] is JsonPayload payload)
                {
                    parameter.Value = payload.Value;
                    dialect.ConfigurePayload(parameter);
                }
                else
                {
                    parameter.Value = args[i] ?? DBNull.Value;
                    if (args[i] == null)
                    {
                        parameter.DbType = System.Data.DbType.String;
                    }
                }

                command.Parameters.Add(parameter);
            }

            return command;
        }
        catch
        {
            command.Dispose();
            throw;
        }
    }
}
