using System;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public static class MySqlDapperConfig
{
    public static void ConfigureTypeHandlers()
    {
        SqlMapper.RemoveTypeMap(typeof(Guid));
        SqlMapper.RemoveTypeMap(typeof(Guid?));
        SqlMapper.AddTypeHandler(new MySqlNullableGuidTypeHandler());
        SqlMapper.AddTypeHandler(new MySqlGuidTypeHandler());
    }
}
