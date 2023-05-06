using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;

[ExcludeFromCodeCoverage]
[Serializable]
public class UserNotFoundException
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

    protected UserNotFoundException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
