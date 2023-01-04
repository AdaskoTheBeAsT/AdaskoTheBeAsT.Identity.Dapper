using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlClient;

[Generator]
public class SqlIdentityDapperSourceGenerator
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public SqlIdentityDapperSourceGenerator()
        : base(new SqlSourceGenerationHelper())
    {
    }
}
