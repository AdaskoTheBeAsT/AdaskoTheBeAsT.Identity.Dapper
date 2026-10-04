using System.Data;
using System.Reflection;
using AwesomeAssertions;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite.IntegrationTest;

public sealed class IdentityQueryMaterializerTest
{
    static IdentityQueryMaterializerTest() => SQLitePCL.Batteries.Init();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DatabaseNullOverwritesConstructorDefaultsWithoutChangingOtherMembers(bool asynchronous)
    {
        using var connection = OpenConnection();
        const string sql = """
            SELECT NULL AS Text, NULL AS Number, NULL AS Bytes, NULL AS PrivateText, NULL AS Required
            UNION ALL
            SELECT 'stored', 23, X'0102', 'private stored', 9;
            """;
        var rows = (asynchronous
            ? await connection.QueryIdentityAsync<DefaultRow>(sql, cancellationToken: TestContext.Current.CancellationToken)
            : connection.QueryIdentity<DefaultRow>(sql)).ToArray();

        rows.Length.Should().Be(2);
        rows[0].Text.Should().BeNull();
        rows[0].Number.Should().BeNull();
        rows[0].Bytes.Should().BeNull();
        rows[0].PrivateText.Should().BeNull();
        rows[0].Required.Should().Be(42);
        rows[0].Unselected.Should().Be("unselected");
        rows[1].Text.Should().Be("stored");
        rows[1].Number.Should().Be(23);
        rows[1].Bytes.Should().Equal(new byte[] { 1, 2 });
        rows[1].PrivateText.Should().Be("private stored");
        rows[1].Required.Should().Be(9);
    }

    [Fact]
    public async Task FirstOrDefaultUsesTheNullPlanAndStillReturnsNullForNoRows()
    {
        using var connection = OpenConnection();
        var row = await connection.QueryIdentityFirstOrDefaultAsync<DefaultRow>(
            "SELECT @text AS Text, @number AS Number;",
            new { text = (string?)null, number = (int?)null }, TestContext.Current.CancellationToken);
        row.Should().NotBeNull();
        row.Text.Should().BeNull();
        row.Number.Should().BeNull();
        row.Required.Should().Be(42);
        (await connection.QueryIdentityFirstOrDefaultAsync<DefaultRow>(
            "SELECT NULL AS Text WHERE 0;", cancellationToken: TestContext.Current.CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task DifferentColumnOrdersAndProjectionsKeepTheirOwnNullOrdinals()
    {
        using var connection = OpenConnection();
        for (var i = 0; i < 3; i++)
        {
            var first = connection.QueryIdentity<DefaultRow>(
                "SELECT NULL AS Text, 23 AS Number;").Should().ContainSingle().Which;
            first.Text.Should().BeNull();
            first.Number.Should().Be(23);

            var reordered = (await connection.QueryIdentityAsync<DefaultRow>(
                "SELECT NULL AS Number, 'reordered' AS Text;", cancellationToken: TestContext.Current.CancellationToken)).Should().ContainSingle().Which;
            reordered.Number.Should().BeNull();
            reordered.Text.Should().Be("reordered");

            var projected = connection.QueryIdentity<DefaultRow>(
                "SELECT 'projected' AS Text;").Should().ContainSingle().Which;
            projected.Text.Should().Be("projected");
            projected.Number.Should().Be(17);
        }
    }

    [Fact]
    public void ParserKeyIncludesColumnTypesAndReusesIdenticalSchemasAcrossReaders()
    {
        var createParser = ParserFactory<SchemaRow>();
        using var intTable = new DataTable();
        intTable.Columns.Add(nameof(SchemaRow.Number), typeof(int));
        intTable.Rows.Add(12);
        using var longTable = new DataTable();
        longTable.Columns.Add(nameof(SchemaRow.Number), typeof(long));
        longTable.Rows.Add(34L);
        using var intReader = intTable.CreateDataReader();
        using var longReader = longTable.CreateDataReader();
        using var anotherIntReader = intTable.CreateDataReader();

        var intParser = createParser(intReader);
        var longParser = createParser(longReader);
        longParser.Should().NotBeSameAs(intParser);
        createParser(anotherIntReader).Should().BeSameAs(intParser);
        intReader.Read().Should().BeTrue();
        longReader.Read().Should().BeTrue();
        intParser(intReader).Number.Should().Be(12);
        longParser(longReader).Number.Should().Be(34);
    }

    [Fact]
    public void ReplacingDapperTypeMapInvalidatesBothParserAndNullAssignments()
    {
        var original = SqlMapper.GetTypeMap(typeof(RemappedRow));
        try
        {
            using var connection = OpenConnection();
            var firstMap = MapAliasTo(nameof(RemappedRow.First));
            SqlMapper.SetTypeMap(typeof(RemappedRow), firstMap);
            var first = connection.QueryIdentity<RemappedRow>("SELECT NULL AS Alias;").Should().ContainSingle().Which;
            first.First.Should().BeNull();
            first.Second.Should().Be("second default");
            var firstValue = connection.QueryIdentity<RemappedRow>("SELECT 'stored' AS Alias;").Should().ContainSingle().Which;
            firstValue.First.Should().Be("stored");

            // Same columns and types, different map reference.
            SqlMapper.SetTypeMap(typeof(RemappedRow), MapAliasTo(nameof(RemappedRow.Second)));
            var second = connection.QueryIdentity<RemappedRow>("SELECT NULL AS Alias;").Should().ContainSingle().Which;
            second.First.Should().Be("first default");
            second.Second.Should().BeNull();
            var secondValue = connection.QueryIdentity<RemappedRow>("SELECT 'stored' AS Alias;").Should().ContainSingle().Which;
            secondValue.First.Should().Be("first default");
            secondValue.Second.Should().Be("stored");

            SqlMapper.SetTypeMap(typeof(RemappedRow), firstMap);
            var restored = connection.QueryIdentity<RemappedRow>("SELECT NULL AS Alias;").Should().ContainSingle().Which;
            restored.First.Should().BeNull();
            restored.Second.Should().Be("second default");
        }
        finally
        {
            SqlMapper.SetTypeMap(typeof(RemappedRow), original);
        }
    }

    [Fact]
    public void CustomMapCanDistinguishColumnNameCase()
    {
        var original = SqlMapper.GetTypeMap(typeof(CaseRow));
        try
        {
            SqlMapper.SetTypeMap(typeof(CaseRow), new CustomPropertyTypeMap(
                typeof(CaseRow), (type, column) => column switch
                {
                    "Alias" => type.GetProperty(nameof(CaseRow.Upper))!,
                    "alias" => type.GetProperty(nameof(CaseRow.Lower))!,
                    _ => null!,
                }));
            using var connection = OpenConnection();
            for (var i = 0; i < 3; i++)
            {
                var upper = connection.QueryIdentity<CaseRow>("SELECT NULL AS Alias;").Should().ContainSingle().Which;
                upper.Upper.Should().BeNull();
                upper.Lower.Should().Be("lower default");
                var lower = connection.QueryIdentity<CaseRow>("SELECT NULL AS alias;").Should().ContainSingle().Which;
                lower.Upper.Should().Be("upper default");
                lower.Lower.Should().BeNull();
            }
        }
        finally
        {
            SqlMapper.SetTypeMap(typeof(CaseRow), original);
        }
    }

    [Fact]
    public void HiddenAndOverriddenPropertiesUseTheExactMappedSetter()
    {
        using var connection = OpenConnection();
        var hidden = connection.QueryIdentity<HiddenRow>("SELECT NULL AS Value;").Should().ContainSingle().Which;
        hidden.Value.Should().BeNull();
        ((HiddenBaseRow)hidden).Value.Should().Be("base default");

        var original = SqlMapper.GetTypeMap(typeof(HiddenRow));
        try
        {
            SqlMapper.SetTypeMap(typeof(HiddenRow), new CustomPropertyTypeMap(
                typeof(HiddenRow), (_, column) => column == "Value"
                    ? typeof(HiddenBaseRow).GetProperty(nameof(HiddenBaseRow.Value))!
                    : null!));
            var mappedBase = connection.QueryIdentity<HiddenRow>("SELECT NULL AS Value;").Should().ContainSingle().Which;
            mappedBase.Value.Should().Be("derived default");
            ((HiddenBaseRow)mappedBase).Value.Should().BeNull();
        }
        finally
        {
            SqlMapper.SetTypeMap(typeof(HiddenRow), original);
        }

        var overridden = connection.QueryIdentity<OverrideRow>("SELECT NULL AS Value;").Should().ContainSingle().Which;
        overridden.Value.Should().BeNull();
        overridden.SetterCalls.Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentQueriesSafelySharePlansWithoutSharingReaders()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 12).Select(worker => Task.Run(async () =>
        {
            await start.Task;
            using var connection = OpenConnection();
            for (var i = 0; i < 24; i++)
            {
                var reverse = (worker + i) % 2 == 0;
                var sql = reverse
                    ? "SELECT NULL AS Number, 'stored' AS Text;"
                    : "SELECT NULL AS Text, 23 AS Number;";
                var row = (i % 3) switch
                {
                    0 => connection.QueryIdentity<ConcurrentRow>(sql).Should().ContainSingle().Which,
                    1 => (await connection.QueryIdentityAsync<ConcurrentRow>(sql)).Should().ContainSingle().Which,
                    _ => (await connection.QueryIdentityFirstOrDefaultAsync<ConcurrentRow>(sql))!,
                };
                row.Text.Should().Be(reverse ? "stored" : null);
                row.Number.Should().Be(reverse ? (int?)null : 23);
            }
        })).ToArray();
        start.SetResult();
        await Task.WhenAll(tasks);
    }

    [Fact]
    public void WarmParserLookupReusesDelegateWithoutManagedAllocations()
    {
        var createParser = ParserFactory<AllocationRow>();
        using var table = new DataTable();
        table.Columns.Add(nameof(AllocationRow.Text), typeof(string));
        using var reader = table.CreateDataReader();
        var expected = createParser(reader);
        for (var i = 0; i < 128; i++)
        {
            createParser(reader);
        }

        var actual = expected;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++)
        {
            actual = createParser(reader);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        actual.Should().BeSameAs(expected);
        allocated.Should().Be(0L);
    }

    [Fact]
    public void PerTypeCacheIsBoundedAndEvictedParsersRemainUsable()
    {
        // Inspect private implementation rather than adding production diagnostics/public test hooks.
        var cache = typeof(IdentityQuery).GetNestedType("ParserCache`1", BindingFlags.NonPublic)!
            .MakeGenericType(typeof(BoundedRow));
        var capacity = (int)cache.GetField("Capacity", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue()!;
        capacity.Should().BeInRange(1, 64);

        var createParser = ParserFactory<BoundedRow>();
        using var firstTable = SchemaTable("Unused0");
        using var firstReader = firstTable.CreateDataReader();
        var firstParser = createParser(firstReader);
        for (var i = 1; i <= capacity; i++)
        {
            using var table = SchemaTable("Unused" + i);
            using var reader = table.CreateDataReader();
            var parse = createParser(reader);
            reader.Read().Should().BeTrue();
            parse(reader).Text.Should().BeNull();
        }

        var entries = (Array)cache.GetField("Entries", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        entries.Length.Should().Be(capacity);
        entries.Cast<object?>().Count(entry => entry != null).Should().Be(capacity);
        createParser(firstReader).Should().NotBeSameAs(firstParser);
        firstReader.Read().Should().BeTrue();
        firstParser(firstReader).Text.Should().BeNull();
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static CustomPropertyTypeMap MapAliasTo(string property) =>
        new(typeof(RemappedRow), (type, column) => column == "Alias" ? type.GetProperty(property)! : null!);

    private static Func<IDataReader, Func<IDataReader, T>> ParserFactory<T>()
        where T : class =>
        (Func<IDataReader, Func<IDataReader, T>>)typeof(IdentityQuery)
            .GetMethod("CreateParser", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(T))
            .CreateDelegate(typeof(Func<IDataReader, Func<IDataReader, T>>));

    private static DataTable SchemaTable(string unusedColumn)
    {
        var table = new DataTable();
        try
        {
            table.Columns.Add(nameof(BoundedRow.Text), typeof(string));
            table.Columns.Add(unusedColumn, typeof(int));
            table.Rows.Add(DBNull.Value, 1);
            return table;
        }
        catch
        {
            table.Dispose();
            throw;
        }
    }

    private sealed class DefaultRow
    {
        public string? Text { get; set; } = "text default";
        public int? Number { get; set; } = 17;
        public byte[]? Bytes { get; set; } = new byte[] { 9 };
        public string? PrivateText { get; private set; } = "private default";
        public int Required { get; set; } = 42;
        public string? Unselected { get; set; } = "unselected";
    }

    private sealed class SchemaRow
    {
        public int? Number { get; set; } = 17;
    }

    private sealed class RemappedRow
    {
        public string? First { get; set; } = "first default";
        public string? Second { get; set; } = "second default";
    }

    private sealed class CaseRow
    {
        public string? Upper { get; set; } = "upper default";
        public string? Lower { get; set; } = "lower default";
    }

    private class HiddenBaseRow
    {
        public string? Value { get; set; } = "base default";
    }

    private sealed class HiddenRow : HiddenBaseRow
    {
        public new string? Value { get; set; } = "derived default";
    }

    private class OverrideBaseRow
    {
        public virtual string? Value { get; set; } = "base default";
    }

    private sealed class OverrideRow : OverrideBaseRow
    {
        private string? _value = "override default";
        public int SetterCalls { get; private set; }

        public override string? Value
        {
            get => _value;
            set
            {
                SetterCalls++;
                _value = value;
            }
        }
    }

    private sealed class ConcurrentRow
    {
        public string? Text { get; set; } = "text default";
        public int? Number { get; set; } = 17;
    }

    private sealed class AllocationRow
    {
        public string? Text { get; set; } = "text default";
    }

    private sealed class BoundedRow
    {
        public string? Text { get; set; } = "text default";
    }
}
