using System;
using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

public class SqliteIdentityHelper
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
                if (insertOwnId)
                {
                    return $@"INSERT INTO {tableName}(
    /**insert**/)
VALUES(
    /**values**/);
SELECT LAST_INSERT_ROWID() AS Id;";
                }

                return $@"WITH new_id AS (
    SELECT lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || '4' || substr(hex(randomblob(2)), 2) || '-' || substr('89ab', 1 + (abs(random()) % 4), 1) || substr(hex(randomblob(2)), 2) || '-' || hex(randomblob(6))) AS id
)
INSERT INTO {tableName}(
    Id,
    /**insert**/)
VALUES(
    (SELECT id FROM new_id),
    /**values**/)
RETURNING (SELECT id FROM new_id) AS Id;";
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
SELECT LAST_INSERT_ROWID() AS Id;";
            case "string":
            case "String":
            case "System.String":
                if (insertOwnId)
                {
                    return $@"INSERT INTO {tableName}(
    /**insert**/)
VALUES(
    /**values**/);
SELECT LAST_INSERT_ROWID() AS Id;";
                }

                return $@"WITH new_id AS (
    SELECT lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || '4' || substr(hex(randomblob(2)), 2) || '-' || substr('89ab', 1 + (abs(random()) % 4), 1) || substr(hex(randomblob(2)), 2) || '-' || hex(randomblob(6))) AS id
)
INSERT INTO {tableName}(
    Id,
    /**insert**/)
VALUES(
    (SELECT id FROM new_id),
    /**values**/)
RETURNING (SELECT id FROM new_id) AS Id;";
            default:
                throw new ArgumentOutOfRangeException(nameof(keyTypeName));
        }
    }
}
