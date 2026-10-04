using System.Data.Common;
using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest.TestCollections;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.IntegrationTest;

public sealed class ConfigurationMatrixTest(DatabaseWithGuidIdFixture fixture)
    : ConfigurationMatrixTestBase, IClassFixture<DatabaseWithGuidIdFixture>
{
    protected override IIncrementalGenerator Generator => new Atb.SqlG.SrcGen();
    protected override DbConnection Connection() => new SqlConnection(fixture.ConnectionString);
    protected override string Provider => "SqlServer";
    protected override string ConnectionType => "Microsoft.Data.SqlClient.SqlConnection";
}
