using System;
using System.Data;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleNullableGuidTypeHandler
    : SqlMapper.TypeHandler<Guid?>
{
    public override Guid? Parse(object value)
    {
        // Check if the value is null or DBNull
        if (value == DBNull.Value)
        {
            return null;
        }

        // Convert byte[] to Guid
        return new Guid((byte[])value);
    }

    public override void SetValue(IDbDataParameter parameter, Guid? value)
    {
        parameter.DbType = DbType.Binary;
        if(value == null)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        // Convert Guid to byte[] and set the parameter value
        parameter.Value = value.Value.ToByteArray();
    }
}
