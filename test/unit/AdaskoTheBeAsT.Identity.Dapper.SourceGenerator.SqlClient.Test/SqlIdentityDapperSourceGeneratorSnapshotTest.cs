namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.SqlClient.Test;

[UsesVerify]
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
}

public class ApplicationRole
    : IdentityRole<Guid>
{
    [Column(""IsActive"")]
    public bool Active { get; set; }
}
";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.VerifyAsync(source);
    }
}
