//HintName: OracleDapperConfig.g.cs
using System;
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
        SqlMapper.RemoveTypeMap(typeof(bool));
        SqlMapper.RemoveTypeMap(typeof(bool?));
        SqlMapper.AddTypeHandler(new GuidRaw16TypeHandler());
        SqlMapper.AddTypeHandler(new NullableGuidRaw16TypeHandler());
        global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(typeof(DateTimeOffset), new DateTimeOffsetTypeHandler());
        global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(typeof(DateTimeOffset?), new NullableDateTimeOffsetTypeHandler());
        global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(typeof(bool), new BooleanCharTypeHandler(StringComparison.OrdinalIgnoreCase));
        global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(
            typeof(bool?),
            new NullableBooleanCharTypeHandler(StringComparison.OrdinalIgnoreCase));
    }
}
