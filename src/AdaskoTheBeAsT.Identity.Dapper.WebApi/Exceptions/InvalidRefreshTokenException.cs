using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;

[ExcludeFromCodeCoverage]
[Serializable]
public class InvalidRefreshTokenException
    : Exception
{
    public InvalidRefreshTokenException()
    {
    }

    public InvalidRefreshTokenException(string? message)
        : base(message)
    {
    }

    public InvalidRefreshTokenException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected InvalidRefreshTokenException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
