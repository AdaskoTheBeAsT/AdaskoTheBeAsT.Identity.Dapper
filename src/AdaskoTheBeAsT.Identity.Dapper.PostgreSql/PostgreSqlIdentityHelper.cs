using System;
using System.Collections.Generic;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Abstractions;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

public class PostgreSqlIdentityHelper
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
/**values**/)
RETURNING Id AS """"Id"""";";
            case "string":
            case "String":
            case "System.String":
                if (insertOwnId)
                {
                    return $@"INSERT INTO {tableName}(
/**insert**/)
VALUES(
/**values**/);
SELECT @Id AS """"Id"""";";
                }

                return $@"DO $$ 
DECLARE
    id UUID := gen_random_uuid();
BEGIN
    INSERT INTO {{tableName}}(
    Id,
    /**insert**/)
    VALUES(
    id::text,
    /**values**/);
    SELECT id::text AS """"Id"""";
END $$;";
            default:
                throw new ArgumentOutOfRangeException(nameof(keyTypeName));
        }
    }
}
