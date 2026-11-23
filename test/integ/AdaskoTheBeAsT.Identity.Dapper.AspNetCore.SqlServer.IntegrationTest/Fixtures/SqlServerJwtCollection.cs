using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.IntegrationTest.Fixtures;

[CollectionDefinition(Name)]
public sealed class SqlServerJwtCollection : ICollectionFixture<SqlServerJwtFixture>
{
    public const string Name = nameof(SqlServerJwtCollection);
}
