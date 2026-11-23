using AdaskoTheBeAsT.Identity.Dapper.MySql;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace Atb.MySqlConnector;

[Generator]
public class SrcGen
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public SrcGen()
        : base(new MySqlSourceGenerationHelper())
    {
    }
}
