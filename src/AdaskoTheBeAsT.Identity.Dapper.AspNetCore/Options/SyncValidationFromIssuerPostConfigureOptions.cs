using System;
using Microsoft.Extensions.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

internal sealed class SyncValidationFromIssuerPostConfigureOptions : IPostConfigureOptions<IdentityJwtValidationOptions>
{
    private readonly IOptionsMonitor<IdentityJwtOptions> _issuerOptions;

    public SyncValidationFromIssuerPostConfigureOptions(IOptionsMonitor<IdentityJwtOptions> issuerOptions)
    {
        ArgumentNullException.ThrowIfNull(issuerOptions);
        _issuerOptions = issuerOptions;
    }

    public void PostConfigure(
        string? name,
        IdentityJwtValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!string.IsNullOrEmpty(name) && !string.Equals(name, Microsoft.Extensions.Options.Options.DefaultName, StringComparison.Ordinal))
        {
            return;
        }

        var source = _issuerOptions.CurrentValue;
        options.AuthenticationScheme = source.AuthenticationScheme;
        options.Issuer = source.Issuer;
        options.ClockSkew = source.ClockSkew;
        options.FailureMode = source.FailureMode;
        options.MapInboundClaims = source.MapInboundClaims;

        options.Audiences.Clear();
        foreach (var audience in source.Audiences)
        {
            options.Audiences.Add(audience);
        }

        options.SigningKeys.Clear();
        foreach (var signingKey in source.SigningKeys)
        {
            options.SigningKeys.Add(signingKey);
        }

        SyncClaimKinds(source.ClaimKinds, options.ClaimKinds);
    }

    private static void SyncClaimKinds(
        IdentityJwtClaimKindOptions source,
        IdentityJwtClaimKindOptions target)
    {
        target.FilterAccessTokenClaimsByKind = source.FilterAccessTokenClaimsByKind;
        target.EnableClaimsTransformation = source.EnableClaimsTransformation;
        target.IncludeRolesInAccessToken = source.IncludeRolesInAccessToken;
        target.CacheDuration = source.CacheDuration;
        target.UseDistributedCache = source.UseDistributedCache;

        target.AccessTokenClaimKinds.Clear();
        foreach (var kind in source.AccessTokenClaimKinds)
        {
            target.AccessTokenClaimKinds.Add(kind);
        }

        target.TransformationClaimKinds.Clear();
        foreach (var kind in source.TransformationClaimKinds)
        {
            target.TransformationClaimKinds.Add(kind);
        }
    }
}
