using System.Runtime.CompilerServices;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlClient.Test;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Enable();
    }
}
