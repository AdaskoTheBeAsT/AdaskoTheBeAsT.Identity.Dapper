using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerIdentityStoreGeneratorBase
    : IdentityStoreGeneratorBase
{
    protected override void GenerateUsing(
        StringBuilder sb,
        string keyTypeName)
    {
        sb.AppendLine("using System;");
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper;");
        sb.AppendLine("using AdaskoTheBeAsT.Identity.Dapper.Abstractions;");
        sb.AppendLine("using Microsoft.AspNetCore.Identity;");
        sb.AppendLine("using Microsoft.Data.SqlClient;");
        sb.AppendLine();
    }
}