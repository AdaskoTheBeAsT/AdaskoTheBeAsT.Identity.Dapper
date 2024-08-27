using System;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.Data.SqlClient;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;

public class IdentityDbConnectionProvider
    : IIdentityDbConnectionProvider<SqlConnection>
{
    private readonly string _connectionString;

    public IdentityDbConnectionProvider(string? connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public SqlConnection Provide()
    {
        return new SqlConnection(_connectionString);
    }
}
