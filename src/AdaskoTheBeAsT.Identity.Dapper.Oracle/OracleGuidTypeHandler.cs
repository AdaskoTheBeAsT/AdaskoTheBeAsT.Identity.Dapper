using System;
using System.Data;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

/// <summary>
/// OracleGuidTypeHandler Invoke with below code due to Oracle not having netstandard2.0 version.
/// </summary>
/// <code>
/// <![CDATA[
/// SqlMapper.AddTypeHandler(typeof(Guid), new OracleGuidTypeHandler(
///     p =>
///     {
///         if (p is OracleParameter oracleParameter)
///         {
///             oracleParameter.OracleDbType = OracleDbType.Raw;
///         }
///     }));
/// ]]>
/// </code>
public class OracleGuidTypeHandler
    : SqlMapper.TypeHandler<Guid>
{
    private readonly Action<IDbDataParameter> _action;

    public OracleGuidTypeHandler(Action<IDbDataParameter>? action)
    {
        _action = action ?? (_ => { });
    }

    public override Guid Parse(object value)
    {
        if (value is byte[] oldBytes)
        {
            return new Guid(oldBytes);
        }

        return Guid.Empty;
    }

    public override void SetValue(
        IDbDataParameter parameter,
        Guid value)
    {
        _action(parameter);
        parameter.Value = value.ToByteArray();
    }
}
