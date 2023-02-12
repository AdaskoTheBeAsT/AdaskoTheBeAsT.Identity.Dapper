using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.Sqlite;

[Generator]
public class SqliteIdentityDapperSourceGenerator
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public SqliteIdentityDapperSourceGenerator()
        : base(new SqliteSourceGenerationHelper())
    {
    }
}
