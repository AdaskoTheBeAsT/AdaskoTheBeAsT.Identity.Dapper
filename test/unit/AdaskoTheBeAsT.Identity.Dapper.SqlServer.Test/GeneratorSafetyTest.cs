using AdaskoTheBeAsT.Identity.Dapper.Testing;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.Test;

public sealed class GeneratorSafetyTest : GeneratorSafetyTestBase
{
    [Theory]
    [InlineData("Guid", false, "OUTPUT inserted.Id")]
    [InlineData("System.Guid", true, "OUTPUT inserted.Id")]
    [InlineData("int", false, "SELECT CAST(SCOPE_IDENTITY() AS INT);")]
    [InlineData("Int32", false, "SELECT CAST(SCOPE_IDENTITY() AS INT);")]
    [InlineData("System.Int32", false, "SELECT CAST(SCOPE_IDENTITY() AS INT);")]
    [InlineData("uint", false, "SELECT CAST(SCOPE_IDENTITY() AS INT);")]
    [InlineData("UInt32", false, "SELECT CAST(SCOPE_IDENTITY() AS INT);")]
    [InlineData("System.UInt32", false, "SELECT CAST(SCOPE_IDENTITY() AS INT);")]
    [InlineData("long", false, "SELECT CAST(SCOPE_IDENTITY() AS BIGINT);")]
    [InlineData("Int64", false, "SELECT CAST(SCOPE_IDENTITY() AS BIGINT);")]
    [InlineData("System.Int64", false, "SELECT CAST(SCOPE_IDENTITY() AS BIGINT);")]
    [InlineData("ulong", false, "SELECT CAST(SCOPE_IDENTITY() AS BIGINT);")]
    [InlineData("UInt64", false, "SELECT CAST(SCOPE_IDENTITY() AS BIGINT);")]
    [InlineData("USystem.Int64", false, "SELECT CAST(SCOPE_IDENTITY() AS BIGINT);")]
    [InlineData("string", false, "OUTPUT inserted.Id")]
    [InlineData("String", true, "OUTPUT inserted.Id")]
    [InlineData("System.String", false, "OUTPUT inserted.Id")]
    public void SqlServerIdentityHelperGetInsertTemplateReturnsExpectedSql(string keyTypeName, bool insertOwnId, string expectedFragment)
    {
        var helper = new SqlServerIdentityHelper();

        helper.GuidNameSet.Should().Contain("Guid");
        helper.NumberNameSet.Should().Contain("int");
        helper.StringNameSet.Should().Contain("string");

        var template = helper.GetInsertTemplate("AspNetUsers", keyTypeName, insertOwnId);

        template.Should().Contain(expectedFragment);
    }

    [Fact]
    public void SqlServerIdentityHelperGetInsertTemplateThrowsForUnknownKeyType()
    {
        var helper = new SqlServerIdentityHelper();

        var act = () => helper.GetInsertTemplate("AspNetUsers", "short", insertOwnId: false);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("keyTypeName");
    }

    protected override IIncrementalGenerator CreateGenerator() => new Atb.SqlG.SrcGen();
}
