using AdaskoTheBeAsT.Identity.Dapper.PostgreSql;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace Atb.PSql;

[Generator]
public class SrcGen
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public SrcGen()
        : base(new PostgreSqlSourceGenerationHelper())
    {
    }
}
