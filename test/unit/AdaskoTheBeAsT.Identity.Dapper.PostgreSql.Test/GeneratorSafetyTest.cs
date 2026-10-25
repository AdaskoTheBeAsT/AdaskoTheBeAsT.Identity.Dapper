using AdaskoTheBeAsT.Identity.Dapper.Testing;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql.Test;

public sealed class GeneratorSafetyTest : GeneratorSafetyTestBase
{
    [Theory]
    [InlineData("Guid", false, "RETURNING Id AS \"Id\";")]
    [InlineData("System.Guid", true, "RETURNING Id AS \"Id\";")]
    [InlineData("int", false, "RETURNING Id AS \"Id\";")]
    [InlineData("Int32", false, "RETURNING Id AS \"Id\";")]
    [InlineData("System.Int32", false, "RETURNING Id AS \"Id\";")]
    [InlineData("uint", false, "RETURNING Id AS \"Id\";")]
    [InlineData("UInt32", false, "RETURNING Id AS \"Id\";")]
    [InlineData("System.UInt32", false, "RETURNING Id AS \"Id\";")]
    [InlineData("long", false, "RETURNING Id AS \"Id\";")]
    [InlineData("Int64", false, "RETURNING Id AS \"Id\";")]
    [InlineData("System.Int64", false, "RETURNING Id AS \"Id\";")]
    [InlineData("ulong", false, "RETURNING Id AS \"Id\";")]
    [InlineData("UInt64", false, "RETURNING Id AS \"Id\";")]
    [InlineData("USystem.Int64", false, "RETURNING Id AS \"Id\";")]
    [InlineData("string", false, "gen_random_uuid()::text")]
    [InlineData("String", true, "RETURNING Id AS \"Id\";")]
    [InlineData("System.String", false, "gen_random_uuid()::text")]
    public void PostgreSqlIdentityHelperGetInsertTemplateReturnsExpectedSql(string keyTypeName, bool insertOwnId, string expectedFragment)
    {
        var helper = new PostgreSqlIdentityHelper();

        helper.GuidNameSet.Should().Contain("Guid");
        helper.NumberNameSet.Should().Contain("int");
        helper.StringNameSet.Should().Contain("string");

        var template = helper.GetInsertTemplate("AspNetUsers", keyTypeName, insertOwnId);

        template.Should().Contain(expectedFragment);
    }

    [Fact]
    public void PostgreSqlIdentityHelperGetInsertTemplateThrowsForUnknownKeyType()
    {
        var helper = new PostgreSqlIdentityHelper();

        var act = () => helper.GetInsertTemplate("AspNetUsers", "short", insertOwnId: false);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("keyTypeName");
    }

    protected override IIncrementalGenerator CreateGenerator() => new Atb.PSql.SrcGen();
}
