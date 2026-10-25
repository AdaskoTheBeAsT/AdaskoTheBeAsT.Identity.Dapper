using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AwesomeAssertions;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Testing;

public sealed class IdentityDapperConfigurationTest
{
    private const string IdentityUserEntity = "IdentityUser";
    private const string ConsumerNamespace = "Consumer";

    [Theory]
    [InlineData(null, "Guid", ConsumerNamespace, "", "baseTypeName")]
    [InlineData(IdentityUserEntity, null, ConsumerNamespace, "", "keyTypeName")]
    [InlineData(IdentityUserEntity, "Guid", null, "", "namespaceName")]
    [InlineData(IdentityUserEntity, "Guid", ConsumerNamespace, null, "schemaPart")]
    public void ConstructorThrowsArgumentNullExceptionWhenRequiredArgumentIsNull(
        string? baseTypeName,
        string? keyTypeName,
        string? namespaceName,
        string? schemaPart,
        string expectedParamName)
    {
        var act = () => new IdentityDapperConfiguration(
            baseTypeName!,
            keyTypeName!,
            namespaceName!,
            schemaPart!,
            skipNormalized: false,
            insertOwnId: false);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(expectedParamName);
    }

    [Fact]
    public void ColumnReturnsMappedColumnOrFallsBackToPropertyName()
    {
        var config = new IdentityDapperConfiguration(
            IdentityUserEntity,
            "Guid",
            ConsumerNamespace,
            "dbo.",
            skipNormalized: true,
            insertOwnId: true)
        {
            Provider = DatabaseProvider.Oracle,
            ColumnMappings = new Dictionary<string, IDictionary<string, string>>(StringComparer.Ordinal)
            {
                [IdentityUserEntity] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["UserName"] = "LoginName",
                    ["EmptyMapped"] = string.Empty,
                },
            },
        };

        config.BaseTypeName.Should().Be(IdentityUserEntity);
        config.KeyTypeName.Should().Be("Guid");
        config.NamespaceName.Should().Be(ConsumerNamespace);
        config.SchemaPart.Should().Be("dbo.");
        config.SkipNormalized.Should().BeTrue();
        config.InsertOwnId.Should().BeTrue();
        config.Provider.Should().Be(DatabaseProvider.Oracle);
        config.Column(IdentityUserEntity, "UserName").Should().Be("LoginName");
        config.Column(IdentityUserEntity, "EmptyMapped").Should().Be("EmptyMapped");
        config.Column(IdentityUserEntity, "UnmappedProperty").Should().Be("UnmappedProperty");
        config.Column("IdentityRole", "Name").Should().Be("Name");
    }

    [Theory]
    [InlineData("System.Guid", "Guid")]
    [InlineData("global::System.Guid", "Guid")]
    [InlineData("String", "string")]
    [InlineData("System.String", "string")]
    [InlineData("global::System.String", "string")]
    [InlineData("Int32", "int")]
    [InlineData("System.Int32", "int")]
    [InlineData("global::System.Int32", "int")]
    [InlineData("Int64", "long")]
    [InlineData("System.Int64", "long")]
    [InlineData("global::System.Int64", "long")]
    [InlineData("UInt32", "uint")]
    [InlineData("System.UInt32", "uint")]
    [InlineData("global::System.UInt32", "uint")]
    [InlineData("UInt64", "ulong")]
    [InlineData("System.UInt64", "ulong")]
    [InlineData("global::System.UInt64", "ulong")]
    [InlineData("USystem.Int64", "ulong")]
    [InlineData("CustomKey", "CustomKey")]
    public void ForGenerationNormalizesCanonicalKeyTypeAndPreservesExistingMappings(string rawKeyType, string expectedCanonicalKeyType)
    {
        const string identityRoleEntity = "IdentityRole";
        var config = new IdentityDapperConfiguration(
            identityRoleEntity,
            rawKeyType,
            ConsumerNamespace,
            string.Empty,
            skipNormalized: false,
            insertOwnId: false)
        {
            ColumnMappings = new Dictionary<string, IDictionary<string, string>>(StringComparer.Ordinal)
            {
                [identityRoleEntity] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Name"] = "MappedRoleName",
                },
            },
        };

        var properties = new List<PropertyColumnTypeTriple>
        {
            new("Name", "string", "Name"),
            new("NormalizedName", "string", "CustomNormalizedName"),
            new("ConcurrencyStamp", "string", "ConcurrencyStamp"),
        };

        var generator = new CapturingRoleClassGenerator();
        _ = generator.Generate(config, properties);
        var generated = generator.CapturedConfig!;

        generated.Should().NotBeNull();
        generated.KeyTypeName.Should().Be(expectedCanonicalKeyType);
        generated.Provider.Should().Be(DatabaseProvider.PostgreSql);
        generated.Column(identityRoleEntity, "Name").Should().Be("MappedRoleName");
        generated.Column(identityRoleEntity, "NormalizedName").Should().Be("CustomNormalizedName");
        generated.Column(identityRoleEntity, "ConcurrencyStamp").Should().Be("ConcurrencyStamp");
        config.Column(identityRoleEntity, "NormalizedName").Should().Be("NormalizedName");
    }

    [Fact]
    public void RawStringLiteralExpandsQuoteDelimiterAndIndentsAllLineSeparators()
    {
        var formatted = RawStringLiteral.Format(
            "SELECT \"\"\"Col\"\"\"\r\nFROM T\rWHERE A = 1\nAND B = 2\u0085AND C = 3\u2028AND D = 4\u2029AND E = 5\r",
            "    ");

        formatted.Should().StartWith("\"\"\"\"\r\n    SELECT \"\"\"Col\"\"\"");
        formatted.Should().EndWith("\r\n    \"\"\"\"");
        formatted.Should().Contain("\r\n    FROM T\r    WHERE A = 1\n    AND B = 2\u0085    AND C = 3\u2028    AND D = 4\u2029    AND E = 5\r    ");
    }

    private sealed class CapturingRoleClassGenerator : IdentityRoleClassGeneratorBase
    {
        public IdentityDapperConfiguration? CapturedConfig { get; private set; }

        protected override DatabaseProvider Provider => DatabaseProvider.PostgreSql;

        protected override string ProcessIdentityRoleCreateSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples)
        {
            CapturedConfig = config;
            return "INSERT INTO Roles";
        }

        protected override string ProcessIdentityRoleUpdateSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            "UPDATE Roles SET Name = @Name WHERE Id = @Id";

        protected override string ProcessIdentityRoleDeleteSql(IdentityDapperConfiguration config) =>
            "DELETE FROM Roles WHERE Id = @Id";

        protected override string ProcessIdentityRoleFindByIdSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            "SELECT * FROM Roles WHERE Id = @Id";

        protected override string ProcessIdentityRoleFindByNameSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            "SELECT * FROM Roles WHERE NormalizedName = @NormalizedName";

        protected override string ProcessIdentityRoleGetRolesSql(
            IdentityDapperConfiguration config,
            IList<PropertyColumnTypeTriple> propertyColumnTypeTriples) =>
            "SELECT * FROM Roles";
    }
}
