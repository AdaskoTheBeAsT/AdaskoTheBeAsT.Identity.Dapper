using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AwesomeAssertions;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.Test;

public sealed class DirectSqlGenerationTest
{
    [Fact]
    public void DirectTokenGenerationUsesAnsiQuotingAndExactComparison()
    {
        var generator = new SqliteIdentityUserTokenClassGenerator();
        var properties = generator.GetAllProperties(
            new[]
            {
                new PropertyColumnTypeTriple("Value", "String", "Token]Value"),
                new PropertyColumnTypeTriple("Name", "String", "Token\"Name"),
            },
            insertOwnId: false);
        var config = new IdentityDapperConfiguration(
            "IdentityUserToken",
            "System.String",
            "DirectGeneration",
            string.Empty,
            skipNormalized: false,
            insertOwnId: false);

        var output = generator.Generate(config, properties);

        output.Should().Contain("public class IdentityUserTokenSql : IIdentityUserTokenConcurrencySql");
        output.Should().Contain("\"Token]Value\"");
        output.Should().Contain("\"Token\"\"Name\"");
        output.Should().Contain("CAST(\"Token]Value\" AS BLOB)=CAST(@OriginalValue AS BLOB)");
        output.Should().NotContain("[Token");
        config.Provider.Should().Be(DatabaseProvider.SqlServer);
        config.ColumnMappings.Should().BeEmpty();
    }
}
