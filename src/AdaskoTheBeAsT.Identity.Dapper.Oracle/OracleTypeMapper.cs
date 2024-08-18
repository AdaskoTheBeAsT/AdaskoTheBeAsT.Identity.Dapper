using System;
using Dapper.Oracle;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public static class OracleTypeMapper
{
    public static string MapIdType(string keyTypeName)
    {
        return keyTypeName switch
        {
            "Guid" => "OracleMappingType.Raw",
            "System.Guid" => "OracleMappingType.Raw",
            "int" => "OracleMappingType.Int32",
            "Int32" => "OracleMappingType.Int32",
            "System.Int32" => "OracleMappingType.Int32",
            "uint" => "OracleMappingType.Int32",
            "UInt32" => "OracleMappingType.Int32",
            "System.UInt32" => "OracleMappingType.Int32",
            "long" => "OracleMappingType.Int64",
            "Int64" => "OracleMappingType.Int64",
            "System.Int64" => "OracleMappingType.Int64",
            "ulong" => "OracleMappingType.Int64",
            "UInt64" => "OracleMappingType.Int64",
            "System.UInt64" => "OracleMappingType.Int64",
            "string" => "OracleMappingType.Char",
            "String" => "OracleMappingType.Char",
            "System.String" => "OracleMappingType.Char",
            _ => throw new ArgumentException($"Unknown key type {keyTypeName}"),
        };
    }

    public static string MapIdSize(string keyTypeName)
    {
        return keyTypeName switch
        {
            "Guid" => "16",
            "System.Guid" => "16",
            "int" => "null",
            "Int32" => "null",
            "System.Int32" => "null",
            "uint" => "null",
            "UInt32" => "null",
            "System.UInt32" => "null",
            "long" => "null",
            "Int64" => "null",
            "System.Int64" => "null",
            "ulong" => "null",
            "UInt64" => "null",
            "System.UInt64" => "null",
            "string" => "36",
            "String" => "36",
            "System.String" => "36",
            _ => throw new ArgumentException($"Unknown key type {keyTypeName}"),
        };
    }

    public static string MapParameterEndByStoreBooleanAs(string? storeBooleanAs)
    {
        if (string.Equals(storeBooleanAs, "char", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Char, ParameterDirection.Input, 1);";
        }
        
        if (string.Equals(storeBooleanAs, "number", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Int16, ParameterDirection.Input);";
        }
        
        if (string.Equals(storeBooleanAs, "string", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Varchar2, ParameterDirection.Input, 10);";
        }

        return string.Empty;
    }

    public static string MapParameterEndByTypeName(string? typeName, string? storeBooleanAs)
    {
        if (string.Equals(typeName, "bool", StringComparison.OrdinalIgnoreCase))
        {
            return MapParameterEndByStoreBooleanAs(storeBooleanAs);
        }

        if (string.Equals(typeName, "string", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Varchar2, ParameterDirection.Input, 256);";
        }

        if (string.Equals(typeName, "int", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Int32, ParameterDirection.Input);";
        }

        if (string.Equals(typeName, "decimal", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Decimal, ParameterDirection.Input, precision: 28, scale: 18);";
        }

        return string.Empty;
    }
}
