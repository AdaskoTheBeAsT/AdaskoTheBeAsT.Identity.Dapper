using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AwesomeAssertions;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.Test;

public sealed class OracleTypeMapperTest
{
    private const string RawMappingType = "OracleMappingType.Raw";
    private const string Int32MappingType = "OracleMappingType.Int32";
    private const string Int64MappingType = "OracleMappingType.Int64";
    private const string CharMappingType = "OracleMappingType.Char";
    private const string NullLiteral = "null";
    private const string Int32InputParameterEnd = "OracleMappingType.Int32, ParameterDirection.Input);";
    private const string Int64InputParameterEnd = "OracleMappingType.Int64, ParameterDirection.Input);";
    private const string Int16InputParameterEnd = "OracleMappingType.Int16, ParameterDirection.Input);";
    private const string DoubleInputParameterEnd = "OracleMappingType.Double, ParameterDirection.Input);";
    private const string TimeStampInputParameterEnd = "OracleMappingType.TimeStamp, ParameterDirection.Input);";
    private const string BlobInputParameterEnd = "OracleMappingType.Blob, ParameterDirection.Input);";
    private const string Varchar2InputParameterEnd = "OracleMappingType.Varchar2, ParameterDirection.Input, 256);";
    private const string RawInputParameterEnd = "OracleMappingType.Raw, ParameterDirection.Input, 16);";
    private const string DecimalInputParameterEnd = "OracleMappingType.Decimal, ParameterDirection.Input, precision: 28, scale: 18);";
    private const string CharBoolParameterEnd = "OracleMappingType.Char, ParameterDirection.Input, 1);";
    private const string StringBoolParameterEnd = "OracleMappingType.Varchar2, ParameterDirection.Input, 10);";

    [Theory]
    [InlineData("Guid", RawMappingType)]
    [InlineData("System.Guid", RawMappingType)]
    [InlineData("int", Int32MappingType)]
    [InlineData("Int32", Int32MappingType)]
    [InlineData("System.Int32", Int32MappingType)]
    [InlineData("uint", Int32MappingType)]
    [InlineData("UInt32", Int32MappingType)]
    [InlineData("System.UInt32", Int32MappingType)]
    [InlineData("long", Int64MappingType)]
    [InlineData("Int64", Int64MappingType)]
    [InlineData("System.Int64", Int64MappingType)]
    [InlineData("ulong", Int64MappingType)]
    [InlineData("UInt64", Int64MappingType)]
    [InlineData("System.UInt64", Int64MappingType)]
    [InlineData("string", CharMappingType)]
    [InlineData("String", CharMappingType)]
    [InlineData("System.String", CharMappingType)]
    public void MapIdTypeReturnsExpectedMappingType(string keyTypeName, string expected)
    {
        var result = OracleTypeMapper.MapIdType(keyTypeName);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("decimal")]
    [InlineData("object")]
    public void MapIdTypeThrowsArgumentExceptionForUnknownKeyType(string keyTypeName)
    {
        var act = () => OracleTypeMapper.MapIdType(keyTypeName);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(keyTypeName))
            .WithMessage($"Unknown key type {keyTypeName}*");
    }

    [Theory]
    [InlineData("Guid", "16")]
    [InlineData("System.Guid", "16")]
    [InlineData("int", NullLiteral)]
    [InlineData("Int32", NullLiteral)]
    [InlineData("System.Int32", NullLiteral)]
    [InlineData("uint", NullLiteral)]
    [InlineData("UInt32", NullLiteral)]
    [InlineData("System.UInt32", NullLiteral)]
    [InlineData("long", NullLiteral)]
    [InlineData("Int64", NullLiteral)]
    [InlineData("System.Int64", NullLiteral)]
    [InlineData("ulong", NullLiteral)]
    [InlineData("UInt64", NullLiteral)]
    [InlineData("System.UInt64", NullLiteral)]
    [InlineData("string", "36")]
    [InlineData("String", "36")]
    [InlineData("System.String", "36")]
    public void MapIdSizeReturnsExpectedSize(string keyTypeName, string expected)
    {
        var result = OracleTypeMapper.MapIdSize(keyTypeName);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("decimal")]
    [InlineData("object")]
    public void MapIdSizeThrowsArgumentExceptionForUnknownKeyType(string keyTypeName)
    {
        var act = () => OracleTypeMapper.MapIdSize(keyTypeName);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(keyTypeName))
            .WithMessage($"Unknown key type {keyTypeName}*");
    }

    [Theory]
    [InlineData("char", CharBoolParameterEnd)]
    [InlineData("CHAR", CharBoolParameterEnd)]
    [InlineData("number", Int16InputParameterEnd)]
    [InlineData("NUMBER", Int16InputParameterEnd)]
    [InlineData("numeric", Int16InputParameterEnd)]
    [InlineData("NUMERIC", Int16InputParameterEnd)]
    [InlineData("string", StringBoolParameterEnd)]
    [InlineData("STRING", StringBoolParameterEnd)]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("bit", "")]
    public void MapParameterEndByStoreBooleanAsReturnsExpectedMapping(string? storeBooleanAs, string expected)
    {
        var result = OracleTypeMapper.MapParameterEndByStoreBooleanAs(storeBooleanAs);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("bool", "char", CharBoolParameterEnd)]
    [InlineData("bool?", "CHAR", CharBoolParameterEnd)]
    [InlineData("BOOL", "number", Int16InputParameterEnd)]
    [InlineData("bool?", "numeric", Int16InputParameterEnd)]
    [InlineData("bool", "string", StringBoolParameterEnd)]
    [InlineData("bool", null, "")]
    [InlineData("string", "char", Varchar2InputParameterEnd)]
    [InlineData("string?", "char", Varchar2InputParameterEnd)]
    [InlineData("STRING", "char", Varchar2InputParameterEnd)]
    [InlineData("int", "char", Int32InputParameterEnd)]
    [InlineData("int?", "char", Int32InputParameterEnd)]
    [InlineData("INT", "char", Int32InputParameterEnd)]
    [InlineData("long", "char", Int64InputParameterEnd)]
    [InlineData("long?", "char", Int64InputParameterEnd)]
    [InlineData("LONG", "char", Int64InputParameterEnd)]
    [InlineData("Guid", "char", RawInputParameterEnd)]
    [InlineData("Guid?", "char", RawInputParameterEnd)]
    [InlineData("System.Guid", "char", RawInputParameterEnd)]
    [InlineData("System.Guid?", "char", RawInputParameterEnd)]
    [InlineData("decimal", "char", DecimalInputParameterEnd)]
    [InlineData("decimal?", "char", DecimalInputParameterEnd)]
    [InlineData("DECIMAL", "char", DecimalInputParameterEnd)]
    [InlineData("DateTime", "char", TimeStampInputParameterEnd)]
    [InlineData("DateTime?", "char", TimeStampInputParameterEnd)]
    [InlineData("System.DateTime", "char", TimeStampInputParameterEnd)]
    [InlineData("System.DateTime?", "char", TimeStampInputParameterEnd)]
    [InlineData("DateTimeOffset", "char", TimeStampInputParameterEnd)]
    [InlineData("DateTimeOffset?", "char", TimeStampInputParameterEnd)]
    [InlineData("System.DateTimeOffset", "char", TimeStampInputParameterEnd)]
    [InlineData("System.DateTimeOffset?", "char", TimeStampInputParameterEnd)]
    [InlineData("byte[]", "char", BlobInputParameterEnd)]
    [InlineData("byte[]?", "char", BlobInputParameterEnd)]
    [InlineData("System.Byte[]", "char", BlobInputParameterEnd)]
    [InlineData("System.Byte[]?", "char", BlobInputParameterEnd)]
    [InlineData("short", "char", Int16InputParameterEnd)]
    [InlineData("short?", "char", Int16InputParameterEnd)]
    [InlineData("ushort", "char", Int16InputParameterEnd)]
    [InlineData("ushort?", "char", Int16InputParameterEnd)]
    [InlineData("byte", "char", Int16InputParameterEnd)]
    [InlineData("byte?", "char", Int16InputParameterEnd)]
    [InlineData("sbyte", "char", Int16InputParameterEnd)]
    [InlineData("sbyte?", "char", Int16InputParameterEnd)]
    [InlineData("float", "char", DoubleInputParameterEnd)]
    [InlineData("float?", "char", DoubleInputParameterEnd)]
    [InlineData("double", "char", DoubleInputParameterEnd)]
    [InlineData("double?", "char", DoubleInputParameterEnd)]
    public void MapParameterEndByTypeNameReturnsExpectedMapping(string typeName, string? storeBooleanAs, string expected)
    {
        var result = OracleTypeMapper.MapParameterEndByTypeName(typeName, storeBooleanAs);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("object")]
    [InlineData("System.Uri")]
    [InlineData("uint")]
    [InlineData("ulong")]
    public void MapParameterEndByTypeNameThrowsNotSupportedExceptionForUnsupportedTypes(string? typeName)
    {
        var act = () => OracleTypeMapper.MapParameterEndByTypeName(typeName, "char");

        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Unsupported Oracle property type: {typeName}");
    }

    [Theory]
    [InlineData("bool", true)]
    [InlineData("bool?", true)]
    [InlineData("string", true)]
    [InlineData("string?", true)]
    [InlineData("int", true)]
    [InlineData("int?", true)]
    [InlineData("long", true)]
    [InlineData("long?", true)]
    [InlineData("Guid", true)]
    [InlineData("System.Guid", true)]
    [InlineData("decimal", true)]
    [InlineData("DateTime", true)]
    [InlineData("System.DateTime", true)]
    [InlineData("DateTimeOffset", true)]
    [InlineData("System.DateTimeOffset", true)]
    [InlineData("byte[]", true)]
    [InlineData("System.Byte[]", true)]
    [InlineData("short", true)]
    [InlineData("ushort", true)]
    [InlineData("byte", true)]
    [InlineData("sbyte", true)]
    [InlineData("float", true)]
    [InlineData("double", true)]
    [InlineData("", false)]
    [InlineData("object", false)]
    [InlineData("System.Uri", false)]
    [InlineData("uint", false)]
    [InlineData("ulong", false)]
    public void IsSupportedPropertyTypeReturnsExpectedResult(string typeName, bool expected)
    {
        var result = OracleTypeMapper.IsSupportedPropertyType(typeName);

        result.Should().Be(expected);
    }

    [Fact]
    public void HelperMethodsGenerateAddAndRemoveClaimsAndHandleMissingCustomProperties()
    {
        var sb = new StringBuilder();
        var options = new IdentityDapperOptions(string.Empty, skipNormalized: false, storeBooleanAs: "number");
        var emptyDict = new Dictionary<string, IList<PropertyColumnTypeTriple>>(StringComparer.Ordinal);
        var claimProperties = new List<PropertyColumnTypeTriple>
        {
            new("UserId", "Guid", "UserId"),
            new("ClaimType", "string", "ClaimType"),
            new("ClaimValue", "string", "ClaimValue"),
        };

        OracleApplicationUserHelper.GenerateCreateImpl(emptyDict, options, sb, "uint", insertOwnId: false);
        OracleApplicationUserHelper.GenerateAddClaimsImpl(sb, "Guid", claimProperties, options);
        OracleApplicationUserHelper.GenerateRemoveClaimsImpl(sb, "Guid");

        var output = sb.ToString();
        output.Should().Contain("AddClaimsImplAsync");
        output.Should().Contain("RemoveClaimsImplAsync");
        output.Should().Contain("IdentityUserClaimSql.CreateSql");
        output.Should().Contain("IdentityUserClaimSql.DeleteSql");
        output.Should().Contain(Int16InputParameterEnd);
    }

    [Theory]
    [InlineData("Guid", false, "id := SYS_GUID();")]
    [InlineData("System.Guid", true, "RETURNING Id INTO :OutputId;")]
    [InlineData("int", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("Int32", true, "RETURNING Id INTO :OutputId;")]
    [InlineData("System.Int32", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("uint", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("UInt32", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("System.UInt32", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("long", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("Int64", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("System.Int64", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("ulong", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("UInt64", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("USystem.Int64", false, "RETURNING Id INTO :OutputId;")]
    [InlineData("string", false, "RAWTOHEX(guid_id)")]
    [InlineData("String", true, "RETURNING Id INTO :OutputId;")]
    [InlineData("System.String", false, "RAWTOHEX(guid_id)")]
    public void OracleIdentityHelperGetInsertTemplateReturnsExpectedSql(string keyTypeName, bool insertOwnId, string expectedFragment)
    {
        var helper = new OracleIdentityHelper();

        helper.GuidNameSet.Should().Contain("Guid");
        helper.NumberNameSet.Should().Contain("int");
        helper.StringNameSet.Should().Contain("string");

        var template = helper.GetInsertTemplate("AspNetUsers", keyTypeName, insertOwnId);

        template.Should().Contain(expectedFragment);
    }

    [Fact]
    public void OracleIdentityHelperGetInsertTemplateThrowsForUnknownKeyType()
    {
        var helper = new OracleIdentityHelper();

        var act = () => helper.GetInsertTemplate("AspNetUsers", "short", insertOwnId: false);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("keyTypeName");
    }
}
