//HintName: BooleanCharTypeHandler.g.cs
using System;
using System.Data;
using Dapper.Oracle.TypeHandler;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class BooleanCharTypeHandler
    : TypeHandlerBase<bool>
{
    private readonly StringComparison _comparison;

    public BooleanCharTypeHandler(StringComparison comparison = StringComparison.Ordinal)
    {
        _comparison = comparison;
    }

    public override void SetValue(IDbDataParameter parameter, bool value)
    {
        SetOracleDbTypeOnParameter(parameter, "Char", 1);
        parameter.Value = value ? "Y" : "N";
    }

    public override bool Parse(object value)
    {
        if (value is string text)
        {
            if (text.Equals("Y", _comparison))
            {
                return true;
            }

            if (text.Equals("N", _comparison))
            {
                return false;
            }

            throw new NotSupportedException($"'{text}' was unexpected - expected 'Y' or 'N'");
        }

        throw new NotSupportedException($"Don't know how to convert a {value.GetType()} to a Boolean");
    }
}