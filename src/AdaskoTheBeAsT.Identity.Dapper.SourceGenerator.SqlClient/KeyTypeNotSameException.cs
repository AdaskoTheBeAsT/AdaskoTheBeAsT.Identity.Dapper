using System;
using System.Runtime.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.SqlClient;

[Serializable]
public class KeyTypeNotSameException
    : Exception
{
    public KeyTypeNotSameException()
    {
    }

    public KeyTypeNotSameException(string message)
        : base(message)
    {
    }

    public KeyTypeNotSameException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }

    protected KeyTypeNotSameException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
