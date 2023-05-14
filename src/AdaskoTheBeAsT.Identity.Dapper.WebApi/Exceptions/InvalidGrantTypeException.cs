using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;

[ExcludeFromCodeCoverage]
[Serializable]
public class InvalidGrantTypeException
    : Exception
{
    public InvalidGrantTypeException()
    {
    }

    public InvalidGrantTypeException(string? message)
        : base(message)
    {
    }

    public InvalidGrantTypeException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected InvalidGrantTypeException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
