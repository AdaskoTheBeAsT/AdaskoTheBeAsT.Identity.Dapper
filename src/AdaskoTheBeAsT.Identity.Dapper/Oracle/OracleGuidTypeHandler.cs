using Dapper;
using System.Data;
using System;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleGuidTypeHandler
    : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
    {
        // Check if the value is null or DBNull
        if (value == DBNull.Value)
        {
            return Guid.Empty; // Or return null if you prefer to handle it as nullable Guid
        }

        // Convert byte[] to Guid
        return new Guid((byte[])value);
    }

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.DbType = DbType.Binary;
        parameter.Value = value.ToByteArray();
    }
}
