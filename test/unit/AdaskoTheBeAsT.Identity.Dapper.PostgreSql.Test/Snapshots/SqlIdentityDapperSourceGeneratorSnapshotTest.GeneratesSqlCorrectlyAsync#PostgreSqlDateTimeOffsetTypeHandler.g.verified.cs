//HintName: PostgreSqlDateTimeOffsetTypeHandler.g.cs
using System;
using System.Data;
using System.Globalization;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

public class PostgreSqlDateTimeOffsetTypeHandler
    : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value)
    {
        if (value == DBNull.Value)
        {
            return default;
        }

        return value switch
        {
            DateTimeOffset dto => dto.ToUniversalTime(),
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            string text when DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedOffset) => parsedOffset,
            string text when DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDateTime) => new DateTimeOffset(parsedDateTime, TimeSpan.Zero),
            _ => new DateTimeOffset(
                DateTime.SpecifyKind(
                    Convert.ToDateTime(value, CultureInfo.InvariantCulture),
                    DateTimeKind.Utc)),
        };
    }

    public override void SetValue(
        IDbDataParameter parameter,
        DateTimeOffset value)
    {
        parameter.Value = value.UtcDateTime;
    }
}