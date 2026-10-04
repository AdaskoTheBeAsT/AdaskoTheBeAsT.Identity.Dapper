using AdaskoTheBeAsT.Identity.Dapper.Testing;
using Atb.SqlG;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.Test;

public static class TestHelper
{
    public static Task VerifyAsync(string source)
    {
        var (driver, compilation) = GeneratorCompilation.Run(source, new SrcGen());
        GeneratorCompilation.AssertCompiles(compilation);
        return Verifier.Verify(driver).UseDirectory("Snapshots");
    }
}
