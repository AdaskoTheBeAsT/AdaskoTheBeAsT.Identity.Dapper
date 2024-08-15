using AdaskoTheBeAsT.Identity.Dapper.Oracle;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace Atb.Oracle;

[Generator]
public class SrcGen
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public SrcGen()
        : base(new OracleSourceGenerationHelper())
    {
    }
}
