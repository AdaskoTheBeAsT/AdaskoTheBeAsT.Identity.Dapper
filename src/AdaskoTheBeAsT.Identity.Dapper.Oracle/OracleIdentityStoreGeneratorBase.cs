using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public class OracleIdentityStoreGeneratorBase
    : IdentityStoreGeneratorBase
{
    protected void GenerateNormalizeSqlMethod(StringBuilder sb)
    {
        sb.AppendLine(
            @"        private static string NormalizeSql(string sql)
        {
            return sql.StartsWith(""DECLARE"", StringComparison.OrdinalIgnoreCase) ||
                   sql.StartsWith(""BEGIN"", StringComparison.OrdinalIgnoreCase)
                ? sql
                : sql.Trim().TrimEnd(';');
        }");
        sb.AppendLine();
    }

    protected override void GenerateUsing(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Security.Claims;");
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper;");
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper.Abstractions;");
        sb.AppendLine("using Dapper;");
        sb.AppendLine("using Dapper.Oracle;");
        sb.AppendLine("using Microsoft.AspNetCore.Identity;");
        sb.AppendLine("using Oracle.ManagedDataAccess.Client;");
        sb.AppendLine();
    }
}
