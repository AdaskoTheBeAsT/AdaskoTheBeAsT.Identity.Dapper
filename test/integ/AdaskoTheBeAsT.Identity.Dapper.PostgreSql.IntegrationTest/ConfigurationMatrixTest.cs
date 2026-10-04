using System.Data.Common;
using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest.TestCollections;
using Microsoft.CodeAnalysis;
using Npgsql;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql.IntegrationTest;

public sealed class ConfigurationMatrixTest(DatabaseWithGuidIdFixture fixture)
    : ConfigurationMatrixTestBase, IClassFixture<DatabaseWithGuidIdFixture>
{
    protected override IIncrementalGenerator Generator => new Atb.PSql.SrcGen();

    protected override string Provider => nameof(PostgreSql);

    protected override string ConnectionType => "Npgsql.NpgsqlConnection";

    protected override DbConnection Connection() => new NpgsqlConnection(fixture.ConnectionString);
}
