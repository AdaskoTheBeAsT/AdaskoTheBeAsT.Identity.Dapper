using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

public class PostgreSqlSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public PostgreSqlSourceGenerationHelper()
        : base(
            new PostgreSqlIdentityRoleClassGenerator(),
            new PostgreSqlIdentityRoleClaimClassGenerator(),
            new PostgreSqlIdentityUserClassGenerator(),
            new PostgreSqlIdentityUserClaimClassGenerator(),
            new PostgreSqlIdentityUserLoginClassGenerator(),
            new PostgreSqlIdentityUserRoleClassGenerator(),
            new PostgreSqlIdentityUserTokenClassGenerator(),
            new PostgreSqlIdentityUserRoleClaimClassGenerator(),
            new PostgreSqlApplicationUserOnlyStoreGenerator(),
            new PostgreSqlApplicationUserStoreGenerator(),
            new PostgreSqlApplicationRoleStoreGenerator())
    {
    }

    protected override string GenerateSchemaPart(string dbSchema) =>
        string.IsNullOrEmpty(dbSchema) ? string.Empty : $"{dbSchema}.";

    protected override void GenerateAdditionalFiles(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        GenerateDateTimeOffsetTypeHandler(context);
        GenerateNullableDateTimeOffsetTypeHandler(context);
        GenerateDapperConfig(context);
    }

    private void GenerateDateTimeOffsetTypeHandler(SourceProductionContext context)
    {
        const string content =
            """
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
            """;

        context.AddSource("PostgreSqlDateTimeOffsetTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void GenerateNullableDateTimeOffsetTypeHandler(SourceProductionContext context)
    {
        const string content =
            """
            using System;
            using System.Data;
            using System.Globalization;
            using Dapper;

            namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

            public class PostgreSqlNullableDateTimeOffsetTypeHandler
                : SqlMapper.TypeHandler<DateTimeOffset?>
            {
                public override DateTimeOffset? Parse(object value)
                {
                    if (value == DBNull.Value)
                    {
                        return null;
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
                    DateTimeOffset? value)
                {
                    parameter.Value = value.HasValue ? value.Value.UtcDateTime : DBNull.Value;
                }
            }
            """;

        context.AddSource("PostgreSqlNullableDateTimeOffsetTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void GenerateDapperConfig(SourceProductionContext context)
    {
        const string content =
            """
            using System;
            using Dapper;

            namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

            public static class PostgreSqlDapperConfig
            {
                public static void ConfigureTypeHandlers()
                {
                    SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
                    SqlMapper.RemoveTypeMap(typeof(DateTimeOffset?));
                    SqlMapper.AddTypeHandler(new PostgreSqlNullableDateTimeOffsetTypeHandler());
                    SqlMapper.AddTypeHandler(new PostgreSqlDateTimeOffsetTypeHandler());
                }
            }
            """;

        context.AddSource("PostgreSqlDapperConfig.g.cs", SourceText.From(content, Encoding.UTF8));
    }
}
