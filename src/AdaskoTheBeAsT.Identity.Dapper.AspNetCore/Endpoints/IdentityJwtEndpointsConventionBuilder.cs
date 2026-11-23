using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Endpoints;

public sealed class IdentityJwtEndpointsConventionBuilder : IEndpointConventionBuilder
{
    private readonly IReadOnlyList<IEndpointConventionBuilder> _innerBuilders;

    internal IdentityJwtEndpointsConventionBuilder(IReadOnlyList<IEndpointConventionBuilder> innerBuilders)
    {
        ArgumentNullException.ThrowIfNull(innerBuilders);
        _innerBuilders = innerBuilders;
    }

    public void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);
        foreach (var builder in _innerBuilders)
        {
            builder.Add(convention);
        }
    }

    public void Finally(Action<EndpointBuilder> finallyConvention)
    {
        ArgumentNullException.ThrowIfNull(finallyConvention);
        foreach (var builder in _innerBuilders)
        {
            builder.Finally(finallyConvention);
        }
    }
}
