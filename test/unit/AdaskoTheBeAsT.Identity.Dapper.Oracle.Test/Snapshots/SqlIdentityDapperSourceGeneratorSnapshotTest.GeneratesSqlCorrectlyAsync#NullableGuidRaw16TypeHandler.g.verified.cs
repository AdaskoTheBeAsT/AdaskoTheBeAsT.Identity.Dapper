//HintName: NullableGuidRaw16TypeHandler.g.cs
using Dapper.Oracle.TypeHandler;
using System.Data;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class NullableGuidRaw16TypeHandler
    : TypeHandlerBase<Guid?>
{
    public override void SetValue(IDbDataParameter parameter, Guid? value)
    {
        SetOracleDbTypeOnParameter(parameter, "Raw", 16);
        if (value == null)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        parameter.Value = value.Value.ToByteArray();
    }

    public override Guid? Parse(object value)
    {
        if (value == DBNull.Value)
        {
            return null;
        }

        if (value is byte[] b)
        {
            return new Guid(b);
        }

        return null;
    }
}