using System.Globalization;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;
using AwesomeAssertions;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Testing;

public sealed class SqlBuilderCultureTest
{
    [Theory]
    [InlineData(DbType.SqlServer)]
    [InlineData(DbType.Postgres)]
    [InlineData(DbType.MySql)]
    [InlineData(DbType.Sqlite)]
    [InlineData(DbType.Oracle)]
    public void PagingClausesUseInvariantNumericFormatting(DbType database)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "negative";
        try
        {
            CultureInfo.CurrentCulture = culture;
            var builder = new AdvancedSqlBuilder();
            builder.Skip(-1, database);
            builder.Take(-2, database);
            var sql = builder.AddTemplate("/**skip**/ /**take**/").RawSql;
            sql.Should().Contain("OFFSET -1");
            sql.Should().Contain(database is DbType.SqlServer or DbType.Oracle ? "FETCH NEXT -2 ROWS ONLY" : "LIMIT -2");
            sql.Should().NotContain("negative");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
