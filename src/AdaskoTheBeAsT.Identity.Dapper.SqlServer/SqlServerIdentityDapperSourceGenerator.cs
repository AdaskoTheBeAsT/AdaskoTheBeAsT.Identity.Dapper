using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer;

[Generator]
public class SqlServerIdentityDapperSourceGenerator
    : IdentityDapperSourceGeneratorBase,
        IIncrementalGenerator
{
    public SqlServerIdentityDapperSourceGenerator()
        : base(new SqlServerSourceGenerationHelper())
    {
    }
}
