using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.PostgreSql;

[Generator]
public class PostgreSqlIdentityDapperSourceGenerator
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public PostgreSqlIdentityDapperSourceGenerator()
        : base(new PostgreSqlSourceGenerationHelper())
    {
    }
}
