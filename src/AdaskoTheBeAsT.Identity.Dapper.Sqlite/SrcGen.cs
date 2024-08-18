using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.Sqlite;
using Microsoft.CodeAnalysis;

namespace Atb.Sqlite;

[Generator]
public class SrcGen
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public SrcGen()
        : base(new SqliteSourceGenerationHelper())
    {
    }
}
