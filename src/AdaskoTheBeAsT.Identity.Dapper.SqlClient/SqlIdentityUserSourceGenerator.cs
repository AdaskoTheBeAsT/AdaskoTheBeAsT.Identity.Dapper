using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlClient;

public class SqlIdentityUserSourceGenerator
    : IdentityUserSourceGeneratorBase
{
    protected override string ProcessIdentityUserCreateSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        return sqlBuilder
            .Insert(string.Join("\r\n,", columnNames.Select(s => $"[{s}]")))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                "INSERT INTO dbo.AspNetUsers(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);\r\nSELECT SCOPE_IDENTITY();")
            .RawSql;
    }

    protected override string ProcessIdentityUserUpdateSql(
        IList<string> columnNames,
        IList<string> propertyNames)
    {
        var sqlBuilder = new AdvancedSqlBuilder();
        var minCount = Math.Min(columnNames.Count, propertyNames.Count);
        var list = new List<string>(minCount);
        for (var i = 0; i < minCount; i++)
        {
            list.Add($"[{columnNames[i]}]=@{propertyNames[i]}");
        }

        return sqlBuilder
            .Set2(string.Join("\r\n,", list))
            .Where2("Id=@Id")
            .AddTemplate(
                "UPDATE dbo.AspNetUsers\r\n/**set2**//**where2**/;")
            .RawSql;
    }
}
