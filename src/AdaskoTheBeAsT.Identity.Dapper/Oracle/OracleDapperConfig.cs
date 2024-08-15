using System;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public static class OracleDapperConfig
{
    public static void ConfigureTypeHandlers(BooleanAs booleanAs)
    {
        SqlMapper.RemoveTypeMap(typeof(Guid));
        SqlMapper.RemoveTypeMap(typeof(Guid?));
        SqlMapper.RemoveTypeMap(typeof(bool));
        SqlMapper.RemoveTypeMap(typeof(bool?));
        SqlMapper.AddTypeHandler(new OracleNullableGuidTypeHandler());
        SqlMapper.AddTypeHandler(new OracleGuidTypeHandler());
        switch (booleanAs)
        {
            case BooleanAs.Char:
                SqlMapper.AddTypeHandler(new OracleNullableBooleanAsCharTypeHandler());
                SqlMapper.AddTypeHandler(new OracleBooleanAsCharTypeHandler());
                break;
            case BooleanAs.Byte:
                SqlMapper.AddTypeHandler(new OracleNullableBooleanAsByteTypeHandler());
                SqlMapper.AddTypeHandler(new OracleBooleanAsByteTypeHandler());
                break;
        }
    }
}
