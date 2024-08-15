using System;
using System.Data;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleBooleanAsCharTypeHandler
    : SqlMapper.TypeHandler<bool>
{
    public override void SetValue(
        IDbDataParameter parameter,
        bool value)
    {
        parameter.DbType = DbType.StringFixedLength;
        parameter.Size = 1;
        parameter.Value = value ? "Y" : "N";
    }

    public override bool Parse(object value)
    {
        if (value == DBNull.Value)
        {
            return false;
        }

        if (value == null!)
        {
            return false;
        }

        return value.ToString()?.Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
