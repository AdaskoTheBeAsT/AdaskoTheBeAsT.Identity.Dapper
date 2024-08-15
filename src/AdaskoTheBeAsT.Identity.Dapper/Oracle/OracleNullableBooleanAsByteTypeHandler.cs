using System;
using System.Data;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleNullableBooleanAsByteTypeHandler
    : SqlMapper.TypeHandler<bool?>
{
    public override void SetValue(
        IDbDataParameter parameter,
        bool? value)
    {
        parameter.DbType = DbType.Byte;
        if (value == null)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        parameter.Value = value.Value ? 1 : 0;
    }

    public override bool? Parse(object value)
    {
        if (value == DBNull.Value)
        {
            return null;
        }

        return Convert.ToByte(value) == 1;
    }
}
