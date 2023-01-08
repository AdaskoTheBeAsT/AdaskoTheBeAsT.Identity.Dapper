using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql;

[Generator]
public class MySqlIdentityDapperSourceGenerator
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public MySqlIdentityDapperSourceGenerator()
        : base(new MySqlSourceGenerationHelper())
    {
    }
}
