using System.Data;
using Dapper.Oracle.TypeHandler;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class BooleanCharTypeHandler(StringComparison comparison = StringComparison.Ordinal)
    : TypeHandlerBase<bool>
{
    public override void SetValue(IDbDataParameter parameter, bool value)
    {
        SetOracleDbTypeOnParameter(parameter, "Char", 1);
        parameter.Value = value ? "Y" : "N";
    }

    public override bool Parse(object value)
    {
        if (value is string text)
        {
            if (text.Equals("Y", comparison))
            {
                return true;
            }

            if (text.Equals("N", comparison))
            {
                return false;
            }

            throw new NotSupportedException($"'{text}' was unexpected - expected 'Y' or 'N'");
        }

        throw new NotSupportedException($"Don't know how to convert a {value.GetType()} to a Boolean");
    }
}