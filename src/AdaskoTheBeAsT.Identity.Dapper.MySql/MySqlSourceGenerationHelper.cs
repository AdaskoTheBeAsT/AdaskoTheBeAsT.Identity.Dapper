using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public MySqlSourceGenerationHelper()
        : base(
            new MySqlIdentityRoleClassGenerator(),
            new MySqlIdentityRoleClaimClassGenerator(),
            new MySqlIdentityUserClassGenerator(),
            new MySqlIdentityUserClaimClassGenerator(),
            new MySqlIdentityUserLoginClassGenerator(),
            new MySqlIdentityUserRoleClassGenerator(),
            new MySqlIdentityUserTokenClassGenerator(),
            new MySqlIdentityUserRoleClaimClassGenerator(),
            new MySqlApplicationUserOnlyStoreGenerator(),
            new MySqlApplicationUserStoreGenerator(),
            new MySqlApplicationRoleStoreGenerator())
    {
    }

    protected override string GenerateSchemaPart(string dbSchema) =>
        string.IsNullOrEmpty(dbSchema) ? string.Empty : $"`{dbSchema}`.";

    protected override void GenerateAdditionalFiles(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        GenerateGuidTypeHandler(context, options);
        GenerateNullableGuidTypeHandler(context, options);
        GenerateDateTimeOffsetTypeHandler(context, options);
        GenerateNullableDateTimeOffsetTypeHandler(context, options);
        GenerateDapperConfig(context, options);
    }

    private void GenerateGuidTypeHandler(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        const string content =
            """
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
            """;

        context.AddSource("MySqlGuidTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void GenerateNullableGuidTypeHandler(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        const string content =
            """
            using System;
            using System.Data;
            using Dapper;

            namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

            public class MySqlNullableGuidTypeHandler
                : SqlMapper.TypeHandler<Guid?>
            {
                public override Guid? Parse(object value)
                {
                    if (value == DBNull.Value)
                    {
                        return null;
                    }
            
                    var asString = value?.ToString();
                    if (Guid.TryParse(asString, out var guid))
                    {
                        return guid;
                    }
            
                    return null;
                }
            
                public override void SetValue(
                    IDbDataParameter parameter,
                    Guid? value)
                {
                    if (!value.HasValue)
                    {
                        parameter.Value = DBNull.Value;
                        return;
                    }
            
                    parameter.Value = value!.Value.ToString("D");
                }
            }
            """;

        context.AddSource("MySqlNullableGuidTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void GenerateDateTimeOffsetTypeHandler(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        const string content =
            """
            using System;
            using System.Data;
            using System.Globalization;
            using Dapper;
            
            namespace AdaskoTheBeAsT.Identity.Dapper.MySql;
            
            public class MySqlDateTimeOffsetTypeHandler
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

        context.AddSource("MySqlDateTimeOffsetTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void GenerateNullableDateTimeOffsetTypeHandler(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        const string content =
            """
            using System;
            using System.Data;
            using System.Globalization;
            using Dapper;

            namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

            public class MySqlNullableDateTimeOffsetTypeHandler
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

        context.AddSource("MySqlNullableDateTimeOffsetTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void GenerateDapperConfig(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        const string content =
            """
            using System;
            using Dapper;
            
            namespace AdaskoTheBeAsT.Identity.Dapper.MySql;
            
            public static class MySqlDapperConfig
            {
                public static void ConfigureTypeHandlers()
                {
                    SqlMapper.RemoveTypeMap(typeof(Guid));
                    SqlMapper.RemoveTypeMap(typeof(Guid?));
                    SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
                    SqlMapper.RemoveTypeMap(typeof(DateTimeOffset?));
                    SqlMapper.AddTypeHandler(new MySqlNullableGuidTypeHandler());
                    SqlMapper.AddTypeHandler(new MySqlGuidTypeHandler());
                    SqlMapper.AddTypeHandler(new MySqlNullableDateTimeOffsetTypeHandler());
                    SqlMapper.AddTypeHandler(new MySqlDateTimeOffsetTypeHandler());
                }
            }
            """;

        context.AddSource("MySqlDapperConfig.g.cs", SourceText.From(content, Encoding.UTF8));
    }
}
