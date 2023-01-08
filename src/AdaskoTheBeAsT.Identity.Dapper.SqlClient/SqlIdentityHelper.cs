using System;
using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlClient;

public class SqlIdentityHelper
    : IIdentityHelper
{
    public ISet<string> GuidNameSet { get; } =
        new HashSet<string>(
            new[] { "Guid", "System.Guid" },
            StringComparer.OrdinalIgnoreCase);

    public string GetInsertTemplate(
        string tableName,
        string keyTypeName)
    {
        switch (keyTypeName)
        {
            case "Guid":
            case "System.Guid":
                return $@"INSERT INTO {tableName}(
/**insert**/)
VALUES(
/**values**/)
OUTPUT inserted.Id
VALUES(1);";
            case "int":
            case "Int32":
            case "System.Int32":
            case "uint":
            case "UInt32":
            case "System.UInt32":
            case "long":
            case "Int64":
            case "System.Int64":
            case "ulong":
            case "UInt64":
            case "USystem.Int64":
                return $@"INSERT INTO {tableName}(
/**insert**/)
VALUES(
/**values**/);
SELECT SCOPE_IDENTITY();";
            case "string":
            case "String":
            case "System.String":
                return $@"INSERT INTO {tableName}(
Id,
/**insert**/)
VALUES(
@Id,
/**values**/);
SELECT @Id;";
            default:
                throw new ArgumentOutOfRangeException(nameof(keyTypeName));
        }
    }
}
