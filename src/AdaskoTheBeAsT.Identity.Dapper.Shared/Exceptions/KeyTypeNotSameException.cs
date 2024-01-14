using System;
using System.Runtime.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Exceptions;

[Serializable]
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
public class KeyTypeNotSameException
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
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

#if NETSTANDARD2_0
    protected KeyTypeNotSameException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
#endif
}
