using System;
using System.Data;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlNullableGuidTypeHandler
    : SqlMapper.TypeHandler<Guid?>
{
    public override Guid? Parse(object value)
    {
        if (value == DBNull.Value)
        {
            return null;
        }

        var asString = value?.ToString();
        if (Guid.TryParse(asString, out var guid))
        {
            return guid;
        }

        return null;
    }

    public override void SetValue(
        IDbDataParameter parameter,
        Guid? value)
    {
        if (!value.HasValue)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        parameter.Value = value!.Value.ToString("D");
    }
}
