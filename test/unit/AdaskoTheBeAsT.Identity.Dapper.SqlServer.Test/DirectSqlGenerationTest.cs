using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AwesomeAssertions;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.Test;

public sealed class DirectSqlGenerationTest
{
    [Fact]
    public void DirectGenerationUsesCanonicalSqlWithoutMutatingConfiguration()
    {
        var generator = new SqlServerIdentityUserClassGenerator();
        var properties = generator.GetAllProperties(
            new[] { new PropertyColumnTypeTriple("Active", "Boolean", "IsActive") },
            insertOwnId: false);
        var config = new IdentityDapperConfiguration(
            "IdentityUser",
            "System.Guid",
            "DirectGeneration",
            string.Empty,
            skipNormalized: false,
            insertOwnId: false);

        var output = generator.Generate(config, properties);

        output.Should().Contain("public class IdentityUserSql : IIdentityUserConcurrencySql");
        output.Should().Contain("OUTPUT inserted.[Id]");
        output.Should().Contain("[IsActive] AS [Active]");
        Count(output, "([ConcurrencyStamp]=@OriginalConcurrencyStamp").Should().Be(1);
        Count(output, "([ConcurrencyStamp]=@ConcurrencyStamp").Should().Be(1);
        output.Should().NotContain("NEWSEQUENTIALID");
        config.Provider.Should().Be(DatabaseProvider.SqlServer);
        config.ColumnMappings.Should().BeEmpty();
    }

    [Fact]
    public void DownstreamProcessHooksRemainPartOfGeneration()
    {
        var generator = new HookedUserClassGenerator();
        var properties = generator.GetAllProperties([], insertOwnId: false);
        var output = generator.Generate(
            new IdentityDapperConfiguration(
                "IdentityUser",
                "Guid",
                "DirectGeneration",
                string.Empty,
                skipNormalized: false,
                insertOwnId: false),
            properties);

        output.Should().Contain("SELECT 'hooked' AS [Id];");
        output.Should().Contain("public string UpdateSql");
    }

    private static int Count(string value, string marker)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += marker.Length;
        }

        return count;
    }

    private sealed class HookedUserClassGenerator
        : SqlServerIdentityUserClassGenerator
    {
        protected override string ProcessIdentityUserFindByIdSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            "SELECT 'hooked' AS [Id];";
    }
}
