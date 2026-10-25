using AdaskoTheBeAsT.Identity.Dapper.Testing;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.Test;

public sealed class GeneratorSafetyTest : GeneratorSafetyTestBase
{
    [Theory]
    [InlineData("Guid", false, "randomblob")]
    [InlineData("System.Guid", true, "SELECT @Id AS Id;")]
    [InlineData("int", false, "LAST_INSERT_ROWID()")]
    [InlineData("Int32", false, "LAST_INSERT_ROWID()")]
    [InlineData("System.Int32", false, "LAST_INSERT_ROWID()")]
    [InlineData("uint", false, "LAST_INSERT_ROWID()")]
    [InlineData("UInt32", false, "LAST_INSERT_ROWID()")]
    [InlineData("System.UInt32", false, "LAST_INSERT_ROWID()")]
    [InlineData("long", false, "LAST_INSERT_ROWID()")]
    [InlineData("Int64", false, "LAST_INSERT_ROWID()")]
    [InlineData("System.Int64", false, "LAST_INSERT_ROWID()")]
    [InlineData("ulong", false, "LAST_INSERT_ROWID()")]
    [InlineData("UInt64", false, "LAST_INSERT_ROWID()")]
    [InlineData("USystem.Int64", false, "LAST_INSERT_ROWID()")]
    [InlineData("string", false, "randomblob")]
    [InlineData("String", true, "SELECT @Id AS Id;")]
    [InlineData("System.String", false, "randomblob")]
    public void SqliteIdentityHelperGetInsertTemplateReturnsExpectedSql(string keyTypeName, bool insertOwnId, string expectedFragment)
    {
        var helper = new SqliteIdentityHelper();

        helper.GuidNameSet.Should().Contain("Guid");
        helper.NumberNameSet.Should().Contain("int");
        helper.StringNameSet.Should().Contain("string");

        var template = helper.GetInsertTemplate("AspNetUsers", keyTypeName, insertOwnId);

        template.Should().Contain(expectedFragment);
    }

    [Fact]
    public void SqliteIdentityHelperGetInsertTemplateThrowsForUnknownKeyType()
    {
        var helper = new SqliteIdentityHelper();

        var act = () => helper.GetInsertTemplate("AspNetUsers", "short", insertOwnId: false);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("keyTypeName");
    }

    protected override IIncrementalGenerator CreateGenerator() => new Atb.Sqlite.SrcGen();
}
