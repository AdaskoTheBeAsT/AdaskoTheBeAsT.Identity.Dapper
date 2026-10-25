using System;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

public static class OracleTypeMapper
{
    private const string OracleMappingTypeInt32 = "OracleMappingType.Int32";
    private const string OracleMappingTypeInt64 = "OracleMappingType.Int64";
    private const string StringTypeName = "string";

    public static bool IsSupportedPropertyType(string typeName)
    {
        try
        {
            MapParameterEndByTypeName(typeName, "char");
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    public static string MapIdType(string keyTypeName)
    {
        return keyTypeName switch
        {
            "Guid" => "OracleMappingType.Raw",
            "System.Guid" => "OracleMappingType.Raw",
            "int" => OracleMappingTypeInt32,
            "Int32" => OracleMappingTypeInt32,
            "System.Int32" => OracleMappingTypeInt32,
            "uint" => OracleMappingTypeInt32,
            "UInt32" => OracleMappingTypeInt32,
            "System.UInt32" => OracleMappingTypeInt32,
            "long" => OracleMappingTypeInt64,
            "Int64" => OracleMappingTypeInt64,
            "System.Int64" => OracleMappingTypeInt64,
            "ulong" => OracleMappingTypeInt64,
            "UInt64" => OracleMappingTypeInt64,
            "System.UInt64" => OracleMappingTypeInt64,
            StringTypeName => "OracleMappingType.Char",
            "String" => "OracleMappingType.Char",
            "System.String" => "OracleMappingType.Char",
            _ => throw new ArgumentException($"Unknown key type {keyTypeName}", nameof(keyTypeName)),
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
            StringTypeName => "36",
            "String" => "36",
            "System.String" => "36",
            _ => throw new ArgumentException($"Unknown key type {keyTypeName}", nameof(keyTypeName)),
        };
    }

    public static string MapParameterEndByStoreBooleanAs(string? storeBooleanAs)
    {
        if (string.Equals(storeBooleanAs, "char", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Char, ParameterDirection.Input, 1);";
        }

        if (string.Equals(storeBooleanAs, "number", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(storeBooleanAs, "numeric", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Int16, ParameterDirection.Input);";
        }

        if (string.Equals(storeBooleanAs, StringTypeName, StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Varchar2, ParameterDirection.Input, 10);";
        }

        return string.Empty;
    }

    public static string MapParameterEndByTypeName(string? typeName, string? storeBooleanAs)
    {
        typeName = typeName?.TrimEnd('?');
        if (string.Equals(typeName, "bool", StringComparison.OrdinalIgnoreCase))
        {
            return MapParameterEndByStoreBooleanAs(storeBooleanAs);
        }

        if (string.Equals(typeName, StringTypeName, StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Varchar2, ParameterDirection.Input, 256);";
        }

        if (string.Equals(typeName, "int", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Int32, ParameterDirection.Input);";
        }

        if (string.Equals(typeName, "long", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Int64, ParameterDirection.Input);";
        }

        if (typeName is "Guid" or "System.Guid")
        {
            return "OracleMappingType.Raw, ParameterDirection.Input, 16);";
        }

        if (string.Equals(typeName, "decimal", StringComparison.OrdinalIgnoreCase))
        {
            return "OracleMappingType.Decimal, ParameterDirection.Input, precision: 28, scale: 18);";
        }

        if (typeName is "DateTime" or "System.DateTime" or "DateTimeOffset" or "System.DateTimeOffset")
        {
            return "OracleMappingType.TimeStamp, ParameterDirection.Input);";
        }

        if (typeName is "byte[]" or "System.Byte[]")
        {
            return "OracleMappingType.Blob, ParameterDirection.Input);";
        }

        if (typeName is "short" or "ushort" or "byte" or "sbyte")
        {
            return "OracleMappingType.Int16, ParameterDirection.Input);";
        }

        if (typeName is "float" or "double")
        {
            return "OracleMappingType.Double, ParameterDirection.Input);";
        }

        throw new NotSupportedException($"Unsupported Oracle property type: {typeName}");
    }
}
