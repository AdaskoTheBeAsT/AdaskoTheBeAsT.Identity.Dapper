using System;
using System.Collections.Generic;
using System.Linq;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Cryptography;

public sealed class DefaultJwtValidationKeysProvider : IJwtValidationKeysProvider, IDisposable
{
    private readonly SecurityKey[] _keys;
    private readonly string[] _algorithms;
    private readonly List<IDisposable> _ownedResources = new();
    private bool _disposed;

    public DefaultJwtValidationKeysProvider(IOptions<IdentityJwtValidationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var descriptors = options.Value.SigningKeys;
        if (descriptors.Count == 0)
        {
            throw new InvalidOperationException("At least one JwtSigningKeyDescriptor must be configured.");
        }

        var keys = new List<SecurityKey>(descriptors.Count);
        var algorithms = new HashSet<string>(StringComparer.Ordinal);
        foreach (var descriptor in descriptors)
        {
            var (key, owned) = JwtKeyMaterialHelper.CreateSecurityKey(descriptor, requirePrivateKey: false);
            if (owned != null)
            {
                _ownedResources.Add(owned);
            }

            keys.Add(key);
            algorithms.Add(JwtKeyMaterialHelper.MapSecurityAlgorithm(descriptor.Algorithm));
        }

        _keys = keys.ToArray();
        _algorithms = algorithms.ToArray();
    }

    public IReadOnlyList<SecurityKey> GetValidationKeys()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _keys;
    }

    public IReadOnlyList<string> GetValidAlgorithms()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _algorithms;
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
