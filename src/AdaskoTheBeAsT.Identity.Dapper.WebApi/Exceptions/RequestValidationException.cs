using System;
using System.Diagnostics.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Exceptions;

[ExcludeFromCodeCoverage]
public sealed class RequestValidationException : Exception
{
    public RequestValidationException()
    {
    }

    public RequestValidationException(string? message)
        : base(message)
    {
    }

    public RequestValidationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
