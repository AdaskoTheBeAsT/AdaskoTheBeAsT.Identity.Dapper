using System;
using System.Linq;
using System.Security.Claims;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Authorization;

public static class AuthorizationPolicyBuilderExtensions
{
    public static AuthorizationPolicyBuilder RequireMfaAssurance(this AuthorizationPolicyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.RequireAuthenticatedUser()
            .RequireAssertion(context =>
                context.User.Claims.Any(c =>
                    (string.Equals(c.Type, IdentityJwtClaimTypes.AuthenticationMethods, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(c.Type, IdentityJwtClaimTypes.MappedAuthenticationMethods, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(c.Type, ClaimTypes.AuthenticationMethod, StringComparison.OrdinalIgnoreCase)) &&
                    string.Equals(c.Value, "mfa", StringComparison.OrdinalIgnoreCase)));
    }
}
