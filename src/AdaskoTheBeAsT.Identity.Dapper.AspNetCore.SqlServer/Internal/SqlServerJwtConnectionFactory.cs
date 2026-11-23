using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Internal;

public sealed class SqlServerJwtConnectionFactory : ISqlServerJwtConnectionFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<IdentityJwtSqlServerOptions> _optionsMonitor;

    public SqlServerJwtConnectionFactory(
        IServiceProvider serviceProvider,
        IOptionsMonitor<IdentityJwtSqlServerOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        _serviceProvider = serviceProvider;
        _optionsMonitor = optionsMonitor;
    }

    public static Task EnsureOpenAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return connection.State == ConnectionState.Open
            ? Task.CompletedTask
            : connection.OpenAsync(cancellationToken);
    }

    public SqlConnection CreateConnection()
    {
        var options = _optionsMonitor.CurrentValue;
        if (options.ConnectionFactory != null)
        {
            return options.ConnectionFactory(_serviceProvider)
                ?? throw new InvalidOperationException("ConnectionFactory returned a null SqlConnection instance.");
        }

        return new SqlConnection(options.ConnectionString);
    }
}
