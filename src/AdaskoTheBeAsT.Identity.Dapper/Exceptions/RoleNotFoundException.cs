using System;
#if NETSTANDARD2_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace AdaskoTheBeAsT.Identity.Dapper.Exceptions;

[Serializable]
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
public class RoleNotFoundException
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
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

#if NETSTANDARD2_0_OR_GREATER
    protected RoleNotFoundException(
        SerializationInfo serializationInfo,
        StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
#endif
}
