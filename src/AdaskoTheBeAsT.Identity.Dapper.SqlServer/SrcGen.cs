using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.SqlServer;
using Microsoft.CodeAnalysis;

namespace Atb.SqlG;

[Generator]
public class SrcGen
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public SrcGen()
        : base(new SqlServerSourceGenerationHelper())
    {
    }
}
