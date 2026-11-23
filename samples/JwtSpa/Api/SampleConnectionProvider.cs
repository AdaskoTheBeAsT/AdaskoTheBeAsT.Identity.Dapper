using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Microsoft.Data.Sqlite;

namespace JwtSpa.Api;

internal sealed class SampleConnectionProvider(string connectionString) : IIdentityDbConnectionProvider<SqliteConnection>
{
    public SqliteConnection Provide() => new(connectionString);
}
