//HintName: GuidRaw16TypeHandler.g.cs
using System;
using System.Data;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class GuidRaw16TypeHandler
    : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToByteArray();
    }

    public override Guid Parse(object value)
    {
        if (value == DBNull.Value)
        {
            return Guid.Empty;
        }

        if (value is byte[] b)
        {
            return new Guid(b);
        }

        return Guid.Empty;
    }
}