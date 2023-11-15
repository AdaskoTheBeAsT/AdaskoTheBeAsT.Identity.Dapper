using System;
using System.Diagnostics.CodeAnalysis;
#if NETSTANDARD2_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;

[ExcludeFromCodeCoverage]
[Serializable]
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
public class InvalidPasswordException
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
    : Exception
{
    public InvalidPasswordException()
    {
    }

    public InvalidPasswordException(string? message)
        : base(message)
    {
    }

    public InvalidPasswordException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

#if NETSTANDARD2_0_OR_GREATER
    protected InvalidPasswordException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
#endif
}
