using System;
using System.Data;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlGuidTypeHandler
    : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
    {
        if (value == DBNull.Value)
        {
            return Guid.Empty;
        }

        var asString = value?.ToString();
        if (Guid.TryParse(asString, out var guid))
        {
            return guid;
        }

        return Guid.Empty;
    }

    public override void SetValue(
        IDbDataParameter parameter,
        Guid value)
    {
        parameter.Value = value.ToString("D");
    }
}
