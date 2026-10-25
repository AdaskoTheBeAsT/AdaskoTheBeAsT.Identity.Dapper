using AdaskoTheBeAsT.Identity.Dapper.Testing;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.Test;

public sealed class GeneratorSafetyTest : GeneratorSafetyTestBase
{
    [Theory]
    [InlineData("Guid", false, "SET @NewId=UUID();")]
    [InlineData("System.Guid", true, "SELECT @Id AS Id;")]
    [InlineData("int", false, "LAST_INSERT_ID()")]
    [InlineData("Int32", false, "LAST_INSERT_ID()")]
    [InlineData("System.Int32", false, "LAST_INSERT_ID()")]
    [InlineData("uint", false, "LAST_INSERT_ID()")]
    [InlineData("UInt32", false, "LAST_INSERT_ID()")]
    [InlineData("System.UInt32", false, "LAST_INSERT_ID()")]
    [InlineData("long", false, "LAST_INSERT_ID()")]
    [InlineData("Int64", false, "LAST_INSERT_ID()")]
    [InlineData("System.Int64", false, "LAST_INSERT_ID()")]
    [InlineData("ulong", false, "LAST_INSERT_ID()")]
    [InlineData("UInt64", false, "LAST_INSERT_ID()")]
    [InlineData("USystem.Int64", false, "LAST_INSERT_ID()")]
    [InlineData("string", false, "SET @NewId=UUID();")]
    [InlineData("String", true, "SELECT @Id AS Id;")]
    [InlineData("System.String", false, "SET @NewId=UUID();")]
    public void MySqlIdentityHelperGetInsertTemplateReturnsExpectedSql(string keyTypeName, bool insertOwnId, string expectedFragment)
    {
        var helper = new MySqlIdentityHelper();

        helper.GuidNameSet.Should().Contain("Guid");
        helper.NumberNameSet.Should().Contain("int");
        helper.StringNameSet.Should().Contain("string");

        var template = helper.GetInsertTemplate("AspNetUsers", keyTypeName, insertOwnId);

        template.Should().Contain(expectedFragment);
    }

    [Fact]
    public void MySqlIdentityHelperGetInsertTemplateThrowsForUnknownKeyType()
    {
        var helper = new MySqlIdentityHelper();

        var act = () => helper.GetInsertTemplate("AspNetUsers", "short", insertOwnId: false);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("keyTypeName");
    }

    protected override IIncrementalGenerator CreateGenerator() => new Atb.MySql.SrcGen();
}
