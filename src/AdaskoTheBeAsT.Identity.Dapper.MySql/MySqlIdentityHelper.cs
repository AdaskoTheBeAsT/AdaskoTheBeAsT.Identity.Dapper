using System;
using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

public class MySqlIdentityHelper
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
                return $@"SET id=UUID();
INSERT INTO {tableName}(
Id,
/**insert**/)
VALUES(
id
/**values**/);
SELECT id;";
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
SELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER);";
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
