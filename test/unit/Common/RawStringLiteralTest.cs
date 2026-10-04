using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Testing;

public sealed class RawStringLiteralTest
{
    [Theory]
    [InlineData("")]
    [InlineData("SELECT [Id]\r\nFROM AspNetUsers;")]
    [InlineData("  SELECT 1;\n\n    SELECT 2;  \n")]
    [InlineData("\rSELECT 1;\r")]
    [InlineData("\tSELECT 1;\r\n\t\r\n")]
    [InlineData("SELECT '{{value}}', 'C:\\data\\identity';")]
    [InlineData("""""SELECT '"""', '""""';""""")]
    public void GeneratedLiteralPreservesExactSql(string sql)
    {
        var source = $$"""
            class Queries
            {
                public string Sql { get; } = {{RawStringLiteral.Format(sql)}};
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp11), cancellationToken: TestContext.Current.CancellationToken);
        tree.GetDiagnostics(cancellationToken: TestContext.Current.CancellationToken).Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
        var literal = tree.GetRoot(cancellationToken: TestContext.Current.CancellationToken).DescendantNodes().OfType<LiteralExpressionSyntax>().Should().ContainSingle().Which;
        literal.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken).Should().BeTrue();
        literal.Token.ValueText.Should().Be(sql);
    }
}
