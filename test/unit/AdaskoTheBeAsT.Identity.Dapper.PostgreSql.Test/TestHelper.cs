using AdaskoTheBeAsT.Identity.Dapper.Testing;
using Atb.PSql;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql.Test;

public static class TestHelper
{
    public static Task VerifyAsync(string source)
    {
        var (driver, compilation) = GeneratorCompilation.Run(source, new SrcGen());
        GeneratorCompilation.AssertCompiles(compilation);
        return Verifier.Verify(driver).UseDirectory("Snapshots");
    }
}
