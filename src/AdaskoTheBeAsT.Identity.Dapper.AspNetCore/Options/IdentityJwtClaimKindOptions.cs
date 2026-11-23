using System;
using System.Collections.Generic;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Options;

public sealed class IdentityJwtClaimKindOptions
{
    public bool FilterAccessTokenClaimsByKind { get; set; }

    public bool EnableClaimsTransformation { get; set; }

    public bool IncludeRolesInAccessToken { get; set; } = true;

    public ISet<string> AccessTokenClaimKinds { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public ISet<string> TransformationClaimKinds { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(2);

    public bool UseDistributedCache { get; set; }
}
