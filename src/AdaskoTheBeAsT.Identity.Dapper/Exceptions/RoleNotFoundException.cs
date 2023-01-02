using System;
using System.Runtime.Serialization;

namespace AdaskoTheBeAsT.Identity.Dapper.Exceptions;

[Serializable]
public class RoleNotFoundException
    : Exception
{
    public RoleNotFoundException()
    {
    }

    public RoleNotFoundException(string message)
        : base(message)
    {
    }

    public RoleNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected RoleNotFoundException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
