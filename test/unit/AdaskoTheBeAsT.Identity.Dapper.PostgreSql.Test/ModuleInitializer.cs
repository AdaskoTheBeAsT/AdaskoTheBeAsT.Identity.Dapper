using System.Runtime.CompilerServices;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql.Test;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
