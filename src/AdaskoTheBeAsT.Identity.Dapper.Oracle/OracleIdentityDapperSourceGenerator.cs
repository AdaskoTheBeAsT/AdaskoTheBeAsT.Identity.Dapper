using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle;

[Generator]
public class OracleIdentityDapperSourceGenerator
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public OracleIdentityDapperSourceGenerator()
        : base(new OracleSourceGenerationHelper())
    {
    }
}
