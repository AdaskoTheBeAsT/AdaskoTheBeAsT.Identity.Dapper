using System.Text;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlIdentityStoreGeneratorBase
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
        sb.AppendLine("using MySql.Data.MySqlClient;");
        sb.AppendLine();
    }
}
