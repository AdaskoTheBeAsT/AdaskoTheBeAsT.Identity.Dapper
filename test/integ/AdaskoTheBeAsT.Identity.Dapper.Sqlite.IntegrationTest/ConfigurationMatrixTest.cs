using System.Data.Common;
using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest.TestCollections;
using Microsoft.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest;

public sealed class ConfigurationMatrixTest(DatabaseWithGuidIdFixture fixture)
    : ConfigurationMatrixTestBase, IClassFixture<DatabaseWithGuidIdFixture>
{
    protected override IIncrementalGenerator Generator => new Atb.Sqlite.SrcGen();

    protected override string Provider => nameof(Sqlite);

    protected override string ConnectionType => "Microsoft.Data.Sqlite.SqliteConnection";

    protected override DbConnection Connection() => new SqliteConnection(fixture.ConnectionString);
}
