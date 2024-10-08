using System;
using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

public class SqlServerIdentityHelper
    : IIdentityHelper
{
    public ISet<string> GuidNameSet { get; } =
        new HashSet<string>(
            new[] { "Guid", "System.Guid" },
            StringComparer.OrdinalIgnoreCase);

    public ISet<string> NumberNameSet { get; } =
        new HashSet<string>(
            new[]
            {
                "int",
                "Int32",
                "System.Int32",
                "uint",
                "UInt32",
                "System.UInt32",
                "long",
                "Int64",
                "System.Int64",
                "ulong",
                "UInt64",
                "System.UInt64",
            },
            StringComparer.OrdinalIgnoreCase);

    public ISet<string> StringNameSet { get; } =
        new HashSet<string>(
            new[]
            {
                "string",
                "String",
                "System.String",
            },
            StringComparer.OrdinalIgnoreCase);

    public string GetInsertTemplate(
        string tableName,
        string keyTypeName,
        bool insertOwnId)
    {
        switch (keyTypeName)
        {
            case "Guid":
            case "System.Guid":
                return $@"INSERT INTO {tableName}(
/**insert**/)
OUTPUT inserted.Id
VALUES(
/**values**/);";
            case "int":
            case "Int32":
            case "System.Int32":
            case "uint":
            case "UInt32":
            case "System.UInt32":
                return $@"INSERT INTO {tableName}(
/**insert**/)
VALUES(
/**values**/);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
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
SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
            case "string":
            case "String":
            case "System.String":
                if (insertOwnId)
                {
                    return $@"INSERT INTO {tableName}(
/**insert**/)
VALUES(
/**values**/);
SELECT @Id;";
                }

                return $@"DECLARE @Id uniqueidentifier;
SET @Id = NEWSEQUENTIALID();
INSERT INTO {tableName}(
Id,
/**insert**/)
OUTPUT inserted.Id
VALUES(
CAST(@Id AS VARCHAR(36)),
/**values**/);";
            default:
                throw new ArgumentOutOfRangeException(nameof(keyTypeName));
        }
    }
}
