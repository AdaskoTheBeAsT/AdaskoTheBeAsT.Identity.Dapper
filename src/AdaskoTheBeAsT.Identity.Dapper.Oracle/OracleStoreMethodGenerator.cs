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
        sb.Append("        protected override async Task CreateImplAsync(\n            OracleConnection connection,\n            Application").Append(entity).Append(' ').Append(parameter).Append(",\n            CancellationToken cancellationToken)\n        {\n            var sql = Identity").Append(entity).AppendLine("Sql.CreateSql;\n            var parameters = new OracleDynamicParameters { BindByName = true };");

        var idType = OracleTypeMapper.MapIdType(keyTypeName);
        var idSize = OracleTypeMapper.MapIdSize(keyTypeName);
        sb.Append("            parameters.Add(\"OutputId\", dbType: ").Append(idType).Append(", direction: ParameterDirection.ReturnValue, size: ").Append(idSize).AppendLine(");");
        if (insertOwnId)
        {
            GenerateIdParameter(sb, parameter, keyTypeName);
        }
    }

    internal static void GenerateUpdateStart(StringBuilder sb, string entity, string parameter, string keyTypeName)
    {
        sb.Append("        protected override async Task UpdateImplAsync(\n            OracleConnection connection,\n            Application").Append(entity).Append(' ').Append(parameter).Append(",\n            CancellationToken cancellationToken)\n        {\n            var sql = NormalizeSql(Identity").Append(entity).Append("Sql.UpdateSql);\n            var parameters = new OracleDynamicParameters { BindByName = true };\n            var stamp = Guid.NewGuid().ToString();\n            parameters.Add(\"OriginalConcurrencyStamp\", ").Append(parameter).AppendLine(".ConcurrencyStamp, OracleMappingType.Varchar2, ParameterDirection.Input, 256);");
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
            sb.Append("            parameters.Add(\"").Append(item.PropertyName).Append("\", ").Append(OracleApplicationUserHelper.PropertyAccess(parameter, item.PropertyName)).Append(", ").AppendLine(OracleTypeMapper.MapParameterEndByTypeName(item.PropertyType, options.StoreBooleanAs));
        }
    }

    internal static void GenerateOutputId(StringBuilder sb, string parameter, string keyTypeName)
    {
        sb.AppendLine(
            "            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(continueOnCapturedContext: false);");

        if (string.Equals(keyTypeName, "string", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("            ").Append(parameter).AppendLine(".Id = parameters.Get<string>(\"OutputId\").TrimEnd();");
        }
        else if (string.Equals(keyTypeName, "int", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(keyTypeName, "long", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("            ").Append(parameter).Append(".Id = parameters.Get<").Append(keyTypeName).AppendLine(">(\"OutputId\");");
        }
        else if (string.Equals(keyTypeName, "Guid", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("            var idBytes = parameters.Get<byte[]>(\"OutputId\");\n            ").Append(parameter).AppendLine(".Id = new Guid(idBytes);");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateIdParameter(StringBuilder sb, string parameter, string keyTypeName) =>
                sb.Append("            parameters.Add(\"Id\", ").Append(parameter).Append(".Id, ").Append(OracleTypeMapper.MapIdType(keyTypeName)).Append(", ParameterDirection.Input, ").Append(OracleTypeMapper.MapIdSize(keyTypeName)).AppendLine(");");
}
