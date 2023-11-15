using System;
using System.Diagnostics.CodeAnalysis;
#if NETSTANDARD2_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;

[ExcludeFromCodeCoverage]
[Serializable]
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
public class UserNotFoundException
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
    : Exception
{
    public UserNotFoundException()
    {
    }

    public UserNotFoundException(string? message)
        : base(message)
    {
    }

    public UserNotFoundException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

#if NETSTANDARD2_0_OR_GREATER
    protected UserNotFoundException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
#endif
}
