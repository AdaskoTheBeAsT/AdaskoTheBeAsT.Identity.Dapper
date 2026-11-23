using System.Data.Common;
using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest.TestCollections;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;
using Xunit;
#if MYSQL_CONNECTOR
using global::MySqlConnector;
using MySqlGenerator = Atb.MySqlConnector.SrcGen;
#else
using MySql.Data.MySqlClient;
using MySqlGenerator = Atb.MySql.SrcGen;
#endif

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.IntegrationTest;

public sealed class ConfigurationMatrixTest(DatabaseWithGuidIdFixture fixture)
    : ConfigurationMatrixTestBase, IClassFixture<DatabaseWithGuidIdFixture>
{
    protected override IIncrementalGenerator Generator => new MySqlGenerator();

    protected override string Provider => nameof(DatabaseProvider.MySql);

    protected override string ConnectionType => typeof(MySqlConnection).FullName!;

    protected override DbConnection Connection() => new MySqlConnection(
        new MySqlConnectionStringBuilder(fixture.ConnectionString) { AllowUserVariables = true }.ConnectionString);
}
