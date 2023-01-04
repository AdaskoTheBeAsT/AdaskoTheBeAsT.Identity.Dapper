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
            .Insert(string.Join("\r\n,", columnNames))
            .Values(string.Join("\r\n,", propertyNames.Select(s => $"@{s}")))
            .AddTemplate(
                "INSERT INTO dbo.AspNetUsers(\r\n/**insert**/)\r\nVALUES(\r\n/**values**/);\r\nSELECT SCOPE_IDENTITY();")
            .RawSql;
    }
}
