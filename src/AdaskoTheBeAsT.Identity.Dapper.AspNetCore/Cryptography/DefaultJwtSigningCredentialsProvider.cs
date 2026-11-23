using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;

public sealed class DefaultJwtSigningCredentialsProvider : IJwtSigningCredentialsProvider, IDisposable
{
    private readonly SigningCredentials _signingCredentials;
    private readonly List<IDisposable> _ownedResources = new();
    private bool _disposed;

    public DefaultJwtSigningCredentialsProvider(IOptions<IdentityJwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var active = options.Value.SigningKeys.FirstOrDefault(k => k.IsActiveSigningKey)
            ?? throw new InvalidOperationException("No active JwtSigningKeyDescriptor is configured.");

        var (securityKey, owned) = JwtKeyMaterialHelper.CreateSecurityKey(active, requirePrivateKey: true);
        if (owned != null)
        {
            _ownedResources.Add(owned);
        }

        _signingCredentials = new SigningCredentials(
            securityKey,
            JwtKeyMaterialHelper.MapSecurityAlgorithm(active.Algorithm));
    }

    public SigningCredentials GetSigningCredentials()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _signingCredentials;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var resource in _ownedResources)
        {
            resource.Dispose();
        }

        _ownedResources.Clear();
    }
}
