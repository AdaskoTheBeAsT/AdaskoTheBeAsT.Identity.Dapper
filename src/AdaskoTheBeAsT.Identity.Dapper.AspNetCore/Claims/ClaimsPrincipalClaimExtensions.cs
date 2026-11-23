using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

public static class ClaimsPrincipalClaimExtensions
{
    public static IReadOnlyList<string> GetClaimValues(
        this ClaimsPrincipal principal,
        string claimType)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        return principal.Claims
            .Where(claim =>
                string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(claim.Value))
            .Select(claim => claim.Value)
            .ToList();
    }

    public static IReadOnlyList<int> GetInt32ClaimValues(
        this ClaimsPrincipal principal,
        string claimType)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        var values = new List<int>();
        foreach (var claim in principal.Claims)
        {
            if (string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(claim.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                values.Add(parsed);
            }
        }

        return values;
    }

    public static IReadOnlyList<long> GetInt64ClaimValues(
        this ClaimsPrincipal principal,
        string claimType)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        var values = new List<long>();
        foreach (var claim in principal.Claims)
        {
            if (string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase) &&
                long.TryParse(claim.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                values.Add(parsed);
            }
        }

        return values;
    }

    public static IReadOnlyList<Guid> GetGuidClaimValues(
        this ClaimsPrincipal principal,
        string claimType)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        var values = new List<Guid>();
        foreach (var claim in principal.Claims)
        {
            if (string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(claim.Value, CultureInfo.InvariantCulture, out var parsed))
            {
                values.Add(parsed);
            }
        }

        return values;
    }
}
