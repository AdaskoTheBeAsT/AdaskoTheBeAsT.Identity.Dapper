using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

internal static class OracleStoreMethodGenerator
{
    internal static void GenerateCreateStart(
                StringBuilder sb, string entity, string parameter, string keyTypeName, bool insertOwnId)
    {
        sb.AppendLine(
            $$"""
                    protected override async Task CreateImplAsync(
                        OracleConnection connection,
                        Application{{entity}} {{parameter}},
                        CancellationToken cancellationToken)
                    {
                        var sql = Identity{{entity}}Sql.CreateSql;
                        var parameters = new OracleDynamicParameters { BindByName = true };
            """);

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.AppendLine(
            $$"""            parameters.Add("OutputId", dbType: {{idType}}, direction: ParameterDirection.ReturnValue, size: {{idSize}});""");
        if (insertOwnId)
        {
            GenerateIdParameter(sb, parameter, keyTypeName);
        }
    }

    internal static void GenerateUpdateStart(StringBuilder sb, string entity, string parameter, string keyTypeName)
    {
        sb.AppendLine(
            $$"""
                    protected override async Task UpdateImplAsync(
                        OracleConnection connection,
                        Application{{entity}} {{parameter}},
                        CancellationToken cancellationToken)
                    {
                        var sql = NormalizeSql(Identity{{entity}}Sql.UpdateSql);
                        var parameters = new OracleDynamicParameters { BindByName = true };
                        var stamp = Guid.NewGuid().ToString();
                        parameters.Add("OriginalConcurrencyStamp", {{parameter}}.ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);
            """);
        GenerateIdParameter(sb, parameter, keyTypeName);
    }

    internal static void GenerateCustomParameters(
            StringBuilder sb,
            IDictionary<string, IList<PropertyColumnTypeTriple>> typePropertiesDict,
            string entity,
            ISet<string> excludedProperties,
            string parameter,
            IdentityDapperOptions options)
    {
        if (!typePropertiesDict.TryGetValue(entity, out var properties))
        {
            return;
        }

        foreach (var item in properties.Where(e => !excludedProperties.Contains(e.PropertyName)))
        {
            sb.AppendLine(
                $$"""            parameters.Add("{{item.PropertyName}}", {{OracleApplicationUserHelper.PropertyAccess(parameter, item.PropertyName)}}, {{OracleTypeMapper.MapParameterEndByTypeName(item.PropertyType, options.StoreBooleanAs)}}""");
        }
    }

    internal static void GenerateOutputId(StringBuilder sb, string parameter, string keyTypeName)
    {
        sb.AppendLine(
            "            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);");

        if (string.Equals(keyTypeName, "string", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(
                $$"""            {{parameter}}.Id = parameters.Get<string>("OutputId").TrimEnd();""");
        }
        else if (string.Equals(keyTypeName, "int", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(keyTypeName, "long", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(
                $$"""            {{parameter}}.Id = parameters.Get<{{keyTypeName}}>("OutputId");""");
        }
        else if (string.Equals(keyTypeName, "Guid", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(
                $$"""
                            var idBytes = parameters.Get<byte[]>("OutputId");
                            {{parameter}}.Id = new Guid(idBytes);
                """);
        }

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateIdParameter(StringBuilder sb, string parameter, string keyTypeName) =>
                sb.AppendLine(
                    $$"""            parameters.Add("Id", {{parameter}}.Id, {{OracleTypeMapper.MapIdType(keyTypeName)}}, ParameterDirection.Input, {{OracleTypeMapper.MapIdSize(keyTypeName)}});""");
}
