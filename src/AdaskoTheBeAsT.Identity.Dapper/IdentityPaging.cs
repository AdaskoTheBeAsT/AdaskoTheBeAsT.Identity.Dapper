using System;

namespace AdaskoTheBeAsT.Identity.Dapper;

public static class IdentityPaging
{
    public const int MaxPageSize = 1000;

    internal static void Validate(int offset, int pageSize)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }
    }
}
