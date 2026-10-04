using AdaskoTheBeAsT.Identity.Dapper.Testing;
using Microsoft.CodeAnalysis;

namespace AdaskoTheBeAsT.Identity.Dapper.MySql.Test;

public sealed class GeneratorSafetyTest : GeneratorSafetyTestBase
{
    protected override IIncrementalGenerator CreateGenerator() => new Atb.MySql.SrcGen();
}
