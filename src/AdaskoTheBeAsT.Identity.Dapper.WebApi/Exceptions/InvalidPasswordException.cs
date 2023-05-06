using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;

[ExcludeFromCodeCoverage]
[Serializable]
public class InvalidPasswordException
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

    protected InvalidPasswordException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
