using System;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public OracleSourceGenerationHelper()
                : base(
                    new OracleIdentityRoleClassGenerator(),
                    new OracleIdentityRoleClaimClassGenerator(),
                    new OracleIdentityUserClassGenerator(),
                    new OracleIdentityUserClaimClassGenerator(),
                    new OracleIdentityUserLoginClassGenerator(),
                    new OracleIdentityUserRoleClassGenerator(),
                    new OracleIdentityUserTokenClassGenerator(),
                    new OracleIdentityUserRoleClaimClassGenerator(),
                    new OracleApplicationUserOnlyStoreGenerator(),
                    new OracleApplicationUserStoreGenerator(),
                    new OracleApplicationRoleStoreGenerator())
    {
    }

    protected override DatabaseProvider Provider => DatabaseProvider.Oracle;

    protected override string GenerateSchemaPart(string dbSchema) =>
                string.IsNullOrEmpty(dbSchema) ? string.Empty : $"{dbSchema}.";

    protected override void GenerateAdditionalFiles(
                SourceProductionContext context,
                IdentityDapperOptions options)
    {
        GenerateGuidRaw16TypeHandler(context);
        GenerateNullableGuidRaw16TypeHandler(context);
        GenerateDateTimeOffsetTypeHandler(context);
        GenerateNullableDateTimeOffsetTypeHandler(context);
        if (string.Equals(options.StoreBooleanAs, "char", StringComparison.OrdinalIgnoreCase))
        {
            GenerateBooleanCharTypeHandler(context);
            GenerateNullableBooleanCharTypeHandler(context);
        }
        else if (string.Equals(options.StoreBooleanAs, "numeric", StringComparison.OrdinalIgnoreCase))
        {
            GenerateNullableBooleanNumericTypeHandler(context);
        }
        else if (string.Equals(options.StoreBooleanAs, "string", StringComparison.OrdinalIgnoreCase))
        {
            GenerateNullableBooleanStringTypeHandler(context);
        }

        GenerateOracleDapperConfig(context, options);
    }

    private static void GenerateGuidRaw16TypeHandler(
                SourceProductionContext context)
    {
        const string content =
            """
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
            """;

        context.AddSource("GuidRaw16TypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private static void GenerateBooleanCharTypeHandler(
                SourceProductionContext context)
    {
        const string content =
            """
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
            """;

        context.AddSource("BooleanCharTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private static void GenerateNullableBooleanCharTypeHandler(
                SourceProductionContext context)
    {
        const string content =
            """
            using System;
            using System.Data;
            using Dapper.Oracle.TypeHandler;

            namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

            public class NullableBooleanCharTypeHandler
                : TypeHandlerBase<bool?>
            {
                private readonly StringComparison _comparison;

                public NullableBooleanCharTypeHandler(StringComparison comparison = StringComparison.Ordinal)
                {
                    _comparison = comparison;
                }

                public override void SetValue(IDbDataParameter parameter, bool? value)
                {
                    SetOracleDbTypeOnParameter(parameter, "Char", 1);
                    if (value == null)
                    {
                        parameter.Value = DBNull.Value;
                        return;
                    }

                    parameter.Value = value.Value ? "Y" : "N";
                }

                public override bool? Parse(object value)
                {
                    if (value == DBNull.Value)
                    {
                        return null;
                    }

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
            """;

        context.AddSource("NullableBooleanCharTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private static void GenerateNullableBooleanNumericTypeHandler(
                SourceProductionContext context)
    {
        const string content =
            """
            using System;
            using System.Data;
            using Dapper.Oracle.TypeHandler;

            namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

            public class NullableBooleanNumericTypeHandler
                : TypeHandlerBase<bool?>
            {
                public override void SetValue(IDbDataParameter parameter, bool? value)
                {
                    SetOracleDbTypeOnParameter(parameter, "Int16");
                    if (value == null)
                    {
                        parameter.Value = DBNull.Value;
                        return;
                    }

                    parameter.Value = (value.Value ? 1 : 0);
                }

                public override bool? Parse(object value)
                {
                    if (value == DBNull.Value)
                    {
                        return null;
                    }

                    if (value is int intVal)
                    {
                        return intVal != 0;
                    }

                    return null;
                }
            }
            """;

        context.AddSource("NullableBooleanNumericTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

#pragma warning disable MA0051 // Method is too long
    private static void GenerateNullableBooleanStringTypeHandler(
        SourceProductionContext context)
    {
        const string content =
            """
            using System;
            using Dapper.Oracle.TypeHandler;
            using System.Data;

            namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

            public class NullableBooleanStringTypeHandler
                : TypeHandlerBase<bool?>
            {
                private readonly string _trueValue;
                private readonly string _falseValue;
                private readonly StringComparison _comparison;

                public NullableBooleanStringTypeHandler(
                    string trueValue,
                    string falseValue,
                    StringComparison comparison = StringComparison.Ordinal)
                {
                    _trueValue = trueValue;
                    _falseValue = falseValue;
                    _comparison = comparison;
                }

                public override void SetValue(IDbDataParameter parameter, bool? value)
                {
                    SetOracleDbTypeOnParameter(parameter, "Varchar2");
                    if (value == null)
                    {
                        parameter.Value = DBNull.Value;
                        return;
                    }

                    parameter.Value = (value.Value ? _trueValue : _falseValue);
                }

                public override bool? Parse(object value)
                {
                    if (value == DBNull.Value)
                    {
                        return null;
                    }

                    if (value is string text)
                    {
                        if (text.Equals(_trueValue, _comparison))
                        {
                            return true;
                        }
                        if (text.Equals(_falseValue, _comparison))
                        {
                            return false;
                        }

                        throw new NotSupportedException($"'{text}' was unexpected - expected '{_trueValue}' or '{_falseValue}'");
                    }

                    throw new NotSupportedException($"Don't know how to convert a {value.GetType()} to a Boolean");
                }
            }
            """;

        context.AddSource("NullableBooleanStringTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

#pragma warning restore MA0051

    private static void GenerateNullableGuidRaw16TypeHandler(
        SourceProductionContext context)
    {
        const string content =
            """
            using System;
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
            """;

        context.AddSource("NullableGuidRaw16TypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private static void GenerateDateTimeOffsetTypeHandler(
                SourceProductionContext context)
    {
        const string content =
            """
            using System;
            using System.Data;
            using System.Globalization;
            using Dapper.Oracle.TypeHandler;

            namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

            public class DateTimeOffsetTypeHandler
                : TypeHandlerBase<DateTimeOffset>
            {
                public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
                {
                    SetOracleDbTypeOnParameter(parameter, "TimeStamp");
                    parameter.Value = value.UtcDateTime;
                }

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
            }
            """;

        context.AddSource("DateTimeOffsetTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private static void GenerateNullableDateTimeOffsetTypeHandler(
                SourceProductionContext context)
    {
        const string content =
            """
            using System;
            using System.Data;
            using System.Globalization;
            using Dapper.Oracle.TypeHandler;

            namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

            public class NullableDateTimeOffsetTypeHandler
                : TypeHandlerBase<DateTimeOffset?>
            {
                public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
                {
                    SetOracleDbTypeOnParameter(parameter, "TimeStamp");
                    parameter.Value = value.HasValue ? value.Value.UtcDateTime : DBNull.Value;
                }

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
            }
            """;

        context.AddSource("NullableDateTimeOffsetTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

#pragma warning disable MA0051 // Method is too long
    private static void GenerateOracleDapperConfig(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        var sb = new StringBuilder();
        const string content1 =
            """
            using System;
            using Dapper;
            using Dapper.Oracle;
            using Dapper.Oracle.TypeHandler;

            namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

            public static class OracleDapperConfig
            {
                public static void ConfigureTypeHandlers()
                {
                    SqlMapper.RemoveTypeMap(typeof(Guid));
                    SqlMapper.RemoveTypeMap(typeof(Guid?));
                    SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
                    SqlMapper.RemoveTypeMap(typeof(DateTimeOffset?));
            """;

        sb.AppendLine(content1);

        if (string.Equals(options.StoreBooleanAs, "char", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "numeric", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.StoreBooleanAs, "string", StringComparison.OrdinalIgnoreCase))
        {
            var content2 =
                """
                        SqlMapper.RemoveTypeMap(typeof(bool));
                        SqlMapper.RemoveTypeMap(typeof(bool?));
                """;
            sb.AppendLine(content2);
        }

        var content3 =
            """
                    SqlMapper.AddTypeHandler(new GuidRaw16TypeHandler());
                    SqlMapper.AddTypeHandler(new NullableGuidRaw16TypeHandler());
                    global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(typeof(DateTimeOffset), new DateTimeOffsetTypeHandler());
                    global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(typeof(DateTimeOffset?), new NullableDateTimeOffsetTypeHandler());
            """;
        sb.AppendLine(content3);

        if (string.Equals(options.StoreBooleanAs, "char", StringComparison.OrdinalIgnoreCase))
        {
            var content4 =
                """
                        global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(typeof(bool), new BooleanCharTypeHandler(StringComparison.OrdinalIgnoreCase));
                        global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(
                            typeof(bool?),
                            new NullableBooleanCharTypeHandler(StringComparison.OrdinalIgnoreCase));
                """;
            sb.AppendLine(content4);
        }
        else if (string.Equals(options.StoreBooleanAs, "numeric", StringComparison.OrdinalIgnoreCase))
        {
            var content5 =
                """
                        global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(typeof(bool), new BooleanNumericTypeHandler());
                        global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(typeof(bool?), new NullableBooleanNumericTypeHandler());
                """;
            sb.AppendLine(content5);
        }
        else if (string.Equals(options.StoreBooleanAs, "string", StringComparison.OrdinalIgnoreCase))
        {
            var content6 =
                """
                        global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(
                            typeof(bool),
                            new BooleanStringTypeHandler("Yes", "No", StringComparison.OrdinalIgnoreCase));
                        global::Dapper.Oracle.OracleTypeMapper.AddTypeHandler(
                            typeof(bool?),
                            new NullableBooleanStringTypeHandler("Yes", "No", StringComparison.OrdinalIgnoreCase));
                """;
            sb.AppendLine(content6);
        }

        var content7 =
            """
                }
            }
            """;
        sb.AppendLine(content7);

        context.AddSource("OracleDapperConfig.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
#pragma warning restore MA0051
}
