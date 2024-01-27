namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.Test;

public class SqlIdentityDapperSourceGeneratorSnapshotTest
{
    [Fact]
    public Task GeneratesSqlCorrectlyAsync()
    {
        // The source code to test
        const string source = @"
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper.Sample;

public class ApplicationUser
    : IdentityUser<Guid>
{
    [Column(""IsActive"")]
    public bool Active { get; set; }

    public override string? NormalizedUserName { get => UserName; set => UserName = value; }

    public override string? NormalizedEmail { get => Email; set => Email = value; }
}

public class ApplicationRole
    : IdentityRole<Guid>
{
    [Column(""IsActive"")]
    public bool Active { get; set; }

    public override string? NormalizedName { get => Name; set => Name = value; }
}

public class ApplicationRoleClaim
    : IdentityRoleClaim<Guid>
{
}

public class ApplicationUserClaim
    : IdentityUserClaim<Guid>
{
}

public class ApplicationUserLogin
    : IdentityUserLogin<Guid>
{
}

public class ApplicationUserRole
    : IdentityUserRole<Guid>
{
}

public class ApplicationUserToken
    : IdentityUserToken<Guid>
{
}
";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.VerifyAsync(source);
    }
}
