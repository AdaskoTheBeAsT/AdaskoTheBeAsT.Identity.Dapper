using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AdaskoTheBeAsT.Identity.Dapper.Testing;

internal static class GeneratorCompilation
{
    public static (GeneratorDriver Driver, Compilation Compilation) Run(
            string source,
            IIncrementalGenerator generator,
            bool skipNormalized = false,
            string storeBooleanAs = "char",
            string schema = "",
            bool referencesGeneratedTypes = false)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => !string.Equals(path, generator.GetType().Assembly.Location, StringComparison.Ordinal) &&
!string.Equals(path, typeof(GeneratorCompilation).Assembly.Location, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "GeneratedConsumer",
            new[] { CSharpSyntaxTree.ParseText(source, cancellationToken: Xunit.TestContext.Current.CancellationToken) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        if (!referencesGeneratedTypes)
        {
            compilation.GetDiagnostics(Xunit.TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
        }

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            optionsProvider: new OptionsProvider(skipNormalized, storeBooleanAs, schema));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _, Xunit.TestContext.Current.CancellationToken);
        var run = driver.GetRunResult();
        run.Results.Should().AllSatisfy(result => result.Exception.Should().BeNull());
        run.Diagnostics.Should().NotContain(d => d.Id == "CS8785");
        if (!run.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            var generated = run.Results.SelectMany(r => r.GeneratedSources).Select(s => s.HintName).ToArray();
            generated.Should().Contain("ApplicationUserStore.g.cs");
            generated.Should().Contain("ApplicationRoleStore.g.cs");
            generated.Should().Contain("IdentityUserSql.g.cs");
        }

        return (driver, output);
    }

    public static void AssertCompiles(Compilation compilation) =>
                compilation.GetDiagnostics(Xunit.TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

    private sealed class OptionsProvider(bool skipNormalized, string storeBooleanAs, string schema) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(skipNormalized, storeBooleanAs, schema);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }

    private sealed class Options(bool skipNormalized, string storeBooleanAs, string schema) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            value = key switch
            {
                "build_property.AdaskoTheBeAsTIdentityDapper_SkipNormalized" => skipNormalized.ToString(),
                "build_property.AdaskoTheBeAsTIdentityDapper_StoreBooleanAs" => storeBooleanAs,
                "build_property.AdaskoTheBeAsTIdentityDapper_DbSchema" => schema,
                _ => string.Empty,
            };
            return value.Length > 0;
        }
    }
}
