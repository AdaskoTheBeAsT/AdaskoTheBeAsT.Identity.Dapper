using System;
using System.Data;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleNullableBooleanAsCharTypeHandler
    : SqlMapper.TypeHandler<bool?>
{
    public override void SetValue(
        IDbDataParameter parameter,
        bool? value)
    {
        parameter.DbType = DbType.StringFixedLength;
        parameter.Size = 1;
        if (value == null)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        parameter.Value = value.Value ? "Y" : "N";
    }

    public override bool? Parse(object value)
    {
        if (value == DBNull.Value)
        {
            return null;
        }

        if (value == null!)
        {
            return null;
        }

        return value.ToString()?.Equals("Y", StringComparison.OrdinalIgnoreCase) ?? null;
    }
}
