//HintName: OracleDapperConfig.g.cs
using Dapper;
using Dapper.Oracle;
using Dapper.Oracle.TypeHandler;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public static class OracleDapperConfig
{
    public static void ConfigureTypeHandlers()
    {
        SqlMapper.RemoveTypeMap(typeof(Guid));
        SqlMapper.RemoveTypeMap(typeof(Guid?));
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset?));
        SqlMapper.AddTypeHandler(new GuidRaw16TypeHandler());
        SqlMapper.AddTypeHandler(new NullableGuidRaw16TypeHandler());
        OracleTypeMapper.AddTypeHandler(typeof(DateTimeOffset), new DateTimeOffsetTypeHandler());
        OracleTypeMapper.AddTypeHandler(typeof(DateTimeOffset?), new NullableDateTimeOffsetTypeHandler());
        OracleTypeMapper.AddTypeHandler(typeof(bool), new BooleanCharTypeHandler(StringComparison.OrdinalIgnoreCase));
        OracleTypeMapper.AddTypeHandler(
            typeof(bool?),
            new NullableBooleanCharTypeHandler(StringComparison.OrdinalIgnoreCase));
    }
}
