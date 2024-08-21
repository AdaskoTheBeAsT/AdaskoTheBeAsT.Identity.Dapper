using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

public class SqliteSourceGenerationHelper
    : SourceGeneratorHelperBase
{
    public SqliteSourceGenerationHelper()
        : base(
            new SqliteIdentityRoleClassGenerator(),
            new SqliteIdentityRoleClaimClassGenerator(),
            new SqliteIdentityUserClassGenerator(),
            new SqliteIdentityUserClaimClassGenerator(),
            new SqliteIdentityUserLoginClassGenerator(),
            new SqliteIdentityUserRoleClassGenerator(),
            new SqliteIdentityUserTokenClassGenerator(),
            new SqliteIdentityUserRoleClaimClassGenerator(),
            new SqliteApplicationUserOnlyStoreGenerator(),
            new SqliteApplicationUserStoreGenerator(),
            new SqliteApplicationRoleStoreGenerator())
    {
    }

    protected override string GenerateSchemaPart(string dbSchema) => string.Empty;

    protected override void GenerateAdditionalFiles(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        GenerateGuidTypeHandler(context, options);
        GenerateNullableGuidTypeHandler(context, options);
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

            namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

            public class SqliteGuidTypeHandler
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

        context.AddSource("SqliteGuidTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
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

            namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

            public class SqliteNullableGuidTypeHandler
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

        context.AddSource("SqliteNullableGuidTypeHandler.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private void GenerateDapperConfig(
        SourceProductionContext context,
        IdentityDapperOptions options)
    {
        const string content =
            """
            using System;
            using Dapper;

            namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

            public static class SqliteDapperConfig
            {
                public static void ConfigureTypeHandlers()
                {
                    SqlMapper.RemoveTypeMap(typeof(Guid));
                    SqlMapper.RemoveTypeMap(typeof(Guid?));
                    SqlMapper.AddTypeHandler(new SqliteNullableGuidTypeHandler());
                    SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());
                }
            }
            """;

        context.AddSource("SqliteDapperConfig.g.cs", SourceText.From(content, Encoding.UTF8));
    }
}
