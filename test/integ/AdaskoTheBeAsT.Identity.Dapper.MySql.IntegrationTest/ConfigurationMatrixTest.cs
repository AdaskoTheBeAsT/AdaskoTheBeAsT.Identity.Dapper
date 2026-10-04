using System.Data.Common;
using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.TestCollections;
using Microsoft.CodeAnalysis;
using MySql.Data.MySqlClient;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest;

public sealed class ConfigurationMatrixTest(DatabaseWithGuidIdFixture fixture)
    : ConfigurationMatrixTestBase, IClassFixture<DatabaseWithGuidIdFixture>
{
    protected override IIncrementalGenerator Generator => new Atb.MySql.SrcGen();

    protected override string Provider => nameof(MySql);

    protected override string ConnectionType => "MySql.Data.MySqlClient.MySqlConnection";

    protected override DbConnection Connection() => new MySqlConnection(
        new MySqlConnectionStringBuilder(fixture.ConnectionString) { AllowUserVariables = true }.ConnectionString);
}
