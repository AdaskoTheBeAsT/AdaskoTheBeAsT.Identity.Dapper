using System.Data.Common;
using AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;
using AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest.TestCollections;
using Microsoft.CodeAnalysis;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.IntegrationTest;

public sealed class ConfigurationMatrixTest(DatabaseWithGuidIdFixture fixture)
    : ConfigurationMatrixTestBase, IClassFixture<DatabaseWithGuidIdFixture>
{
    protected override IIncrementalGenerator Generator => new Atb.Oracle.SrcGen();
    protected override DbConnection Connection() => new OracleConnection(fixture.ConnectionString);
    protected override string Provider => "Oracle";
    protected override string ConnectionType => "Oracle.ManagedDataAccess.Client.OracleConnection";
}
