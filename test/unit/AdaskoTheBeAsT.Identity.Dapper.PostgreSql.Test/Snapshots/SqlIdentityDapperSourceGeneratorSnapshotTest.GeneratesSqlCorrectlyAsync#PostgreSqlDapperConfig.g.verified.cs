//HintName: PostgreSqlDapperConfig.g.cs
using System;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

public static class PostgreSqlDapperConfig
{
    public static void ConfigureTypeHandlers()
    {
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset?));
        SqlMapper.AddTypeHandler(new PostgreSqlNullableDateTimeOffsetTypeHandler());
        SqlMapper.AddTypeHandler(new PostgreSqlDateTimeOffsetTypeHandler());
    }
}