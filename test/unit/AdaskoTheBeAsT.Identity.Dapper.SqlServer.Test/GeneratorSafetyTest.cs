using AdaskoTheBeAsT.Identity.Dapper.Testing;
using Microsoft.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.SqlServer.Test;

public sealed class GeneratorSafetyTest : GeneratorSafetyTestBase
{
    protected override IIncrementalGenerator CreateGenerator() => new Atb.SqlG.SrcGen();
}
