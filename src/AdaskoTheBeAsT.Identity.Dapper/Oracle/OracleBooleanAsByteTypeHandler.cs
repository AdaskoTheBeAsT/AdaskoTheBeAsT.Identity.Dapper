using System;
using System.Data;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleBooleanAsByteTypeHandler
    : SqlMapper.TypeHandler<bool>
{
    public override void SetValue(
        IDbDataParameter parameter,
        bool value)
    {
        parameter.DbType = DbType.Byte;
        parameter.Value = value ? 1 : 0;
    }

    public override bool Parse(object value)
    {
        if (value == DBNull.Value)
        {
            return false;
        }

        return Convert.ToByte(value) == 1;
    }
}
