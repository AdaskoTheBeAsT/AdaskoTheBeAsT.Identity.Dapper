using System.Text.RegularExpressions;
using AdaskoTheBeAsT.Identity.Dapper.Testing;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Oracle.Test;

public sealed partial class GeneratorSafetyTest : GeneratorSafetyTestBase
{
    [GeneratedRegex(@":(?<bind>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex SqlBindRegex { get; }

    [Theory]
    [InlineData("char")]
    [InlineData("numeric")]
    [InlineData("string")]
    public void BooleanStorageModesCompile(string storage)
    {
        var (driver, compilation) = GeneratorCompilation.Run(Model("Guid", ownId: true), CreateGenerator(), storeBooleanAs: storage);
        GeneratorCompilation.AssertCompiles(compilation);
        var store = Source(driver, "ApplicationUserOnlyStore.g.cs");
        foreach (var property in new[] { "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled" })
        {
            (store.Split($"parameters.Add(\"{property}\",", StringSplitOptions.None).Length - 1).Should().Be(2);
        }
    }

    [Fact]
    public void TokenOverridesForwardCancellationAndBindByName()
    {
        var (driver, compilation) = GeneratorCompilation.Run(Model("Guid", ownId: true), CreateGenerator());
        GeneratorCompilation.AssertCompiles(compilation);
        foreach (var name in new[] { "ApplicationUserOnlyStore.g.cs", "ApplicationUserStore.g.cs" })
        {
            var store = Source(driver, name);
            foreach (var method in new[] { "FindTokenImplAsync", "AddUserTokenImplAsync", "RemoveUserTokenImplAsync", "TryUpdateTokenImplAsync" })
            {
                var start = store.IndexOf(method, StringComparison.Ordinal);
                var next = store.IndexOf("protected override", start, StringComparison.Ordinal);
                var body = next < 0 ? store[start..] : store[start..next];
                body.Should().Contain("new CommandDefinition(");
                body.Should().Contain("cancellationToken: cancellationToken");
                body.Should().Contain("BindByName = true");
            }
        }
    }

    [Theory]
    [InlineData("Guid", false, false)]
    [InlineData("Guid", true, true)]
    [InlineData("string", false, true)]
    [InlineData("string", true, false)]
    [InlineData("int", false, false)]
    [InlineData("int", true, true)]
    [InlineData("long", false, true)]
    [InlineData("long", true, false)]
    public void EveryParameterBagExplicitlyBindsByName(string key, bool skipNormalized, bool ownId)
    {
        var (driver, compilation) = GeneratorCompilation.Run(Model(key, ownId), CreateGenerator(), skipNormalized);
        GeneratorCompilation.AssertCompiles(compilation);
        foreach (var name in new[] { "ApplicationUserOnlyStore.g.cs", "ApplicationUserStore.g.cs", "ApplicationRoleStore.g.cs" })
        {
            var bags = CSharpSyntaxTree.ParseText(Source(driver, name), cancellationToken: TestContext.Current.CancellationToken)
                .GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                .Where(expression => string.Equals(expression.Type.ToString(), "OracleDynamicParameters", StringComparison.Ordinal)).ToArray();
            bags.Should().NotBeEmpty();
            bags.Should().AllSatisfy(bag =>
            {
                var assignment = bag.Initializer!.Expressions.Should().ContainSingle().Which.Should().BeOfType<AssignmentExpressionSyntax>().Which;
                assignment.Left.ToString().Should().Be("BindByName");
                assignment.Right.IsKind(SyntaxKind.TrueLiteralExpression).Should().BeTrue();
            });
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClaimReplacementAndScopedLoginSupplyExactlyTheSqlBindNames(bool skipNormalized)
    {
        var (driver, compilation) = GeneratorCompilation.Run(Model("Guid", ownId: true), CreateGenerator(), skipNormalized);
        GeneratorCompilation.AssertCompiles(compilation);
        foreach (var name in new[] { "ApplicationUserOnlyStore.g.cs", "ApplicationUserStore.g.cs" })
        {
            var methods = CSharpSyntaxTree.ParseText(Source(driver, name), cancellationToken: TestContext.Current.CancellationToken)
                .GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
            var replace = methods.Should().ContainSingle(method => method.Identifier.ValueText == "ReplaceClaimImplAsync").Which;
            AssertSqlBindNames(
driver,
"IdentityUserClaimSql.g.cs",
"ReplaceSql",
replace,
"ClaimTypeNew",
"ClaimTypeOld",
"ClaimValueNew",
"ClaimValueOld",
"UserId");
            var scopedLogin = methods.Should().ContainSingle(method => method.Identifier.ValueText == "FindUserLoginImplAsync" &&
                method.ParameterList.Parameters.Any(parameter => parameter.Identifier.ValueText == "userId")).Which;
            AssertSqlBindNames(
driver,
"IdentityUserLoginSql.g.cs",
"GetByUserIdLoginProviderKeySql",
scopedLogin,
"LoginProvider",
"ProviderKey",
"UserId");
        }
    }

    [Theory]
    [InlineData("event")]
    [InlineData("class")]
    [InlineData("required")]
    public void KeywordCustomMappedPropertiesCompileForEveryEntity(string keyword)
    {
        var member = $$"""[Column("CustomField")] public string? @{{keyword}} { get; set; }""";
        var model = Model("Guid", ownId: true).Replace("public string? DisplayLabel { get; set; }", member, StringComparison.Ordinal);
        foreach (var entity in new[] { "Role", "UserClaim", "RoleClaim", "UserLogin", "UserRole", "UserToken" })
        {
            model = model.Replace(
                $"Application{entity} : Identity{entity}<Guid> {{ }}",
                $$"""Application{{entity}} : Identity{{entity}}<Guid> { {{member}} }""",
                StringComparison.Ordinal);
        }

        var (driver, compilation) = GeneratorCompilation.Run(model, CreateGenerator());
        GeneratorCompilation.AssertCompiles(compilation);
        foreach (var storeName in new[] { "ApplicationUserOnlyStore.g.cs", "ApplicationUserStore.g.cs" })
        {
            var store = Source(driver, storeName);
            (store.Split($"parameters.Add(\"{keyword}\", user.@{keyword},", StringSplitOptions.None).Length - 1).Should().Be(2);
            var expectedEntityBindings = string.Equals(storeName, "ApplicationUserStore.g.cs", StringComparison.Ordinal) ? 2 : 1;
            (store.Split($"parameters.Add(\"{keyword}\", entity.@{keyword},", StringSplitOptions.None).Length - 1).Should().Be(expectedEntityBindings);
            store.Should().Contain($"parameters.Add(name + suffix, entity.@{keyword},");
            store.Should().Contain($"parameters.Add(\"{keyword}\", token.@{keyword},");
        }

        var roles = Source(driver, "ApplicationRoleStore.g.cs");
        (roles.Split($"parameters.Add(\"{keyword}\", role.@{keyword},", StringSplitOptions.None).Length - 1).Should().Be(2);
        roles.Should().Contain($"parameters.Add(\"{keyword}\", roleClaim.@{keyword},");
        foreach (var entity in new[] { "User", "Role", "UserClaim", "RoleClaim", "UserLogin", "UserRole", "UserToken" })
        {
            var sql = Source(driver, $"Identity{entity}Sql.g.cs");
            sql.Should().Contain("CUSTOMFIELD");
            sql.Should().Contain($":{keyword}");
            sql.Should().NotContain($":@{keyword}");
        }
    }

    [Theory]
    [InlineData("char")]
    [InlineData("numeric")]
    [InlineData("string")]
    public void GeneratedConsumerCompilesWithCSharp11(string storage)
    {
        var (_, compilation) = GeneratorCompilation.Run(Model("Guid", ownId: true), CreateGenerator(), storeBooleanAs: storage);
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp11);
        var trees = compilation.SyntaxTrees.Select(tree => CSharpSyntaxTree.ParseText(
            tree.GetText(TestContext.Current.CancellationToken),
            parseOptions,
            tree.FilePath,
            cancellationToken: TestContext.Current.CancellationToken)).ToArray();
        var csharp11 = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(trees);
        csharp11.SyntaxTrees.Should().AllSatisfy(tree => ((CSharpParseOptions)tree.Options).LanguageVersion.Should().Be(LanguageVersion.CSharp11));
        GeneratorCompilation.AssertCompiles(csharp11);
    }

    [Fact]
    public void UnsupportedCustomTypesProduceLocatedDiagnosticInsteadOfCrashing()
    {
        var (driver, _) = GeneratorCompilation.Run(
            Model("Guid", ownId: true).Replace(
            "public string? DisplayLabel { get; set; }", "public System.Uri? Website { get; set; }", StringComparison.Ordinal),
            CreateGenerator());
        var diagnostic = driver.GetRunResult().Diagnostics.Where(d => string.Equals(d.Id, "ATBID104", StringComparison.Ordinal)).Should().ContainSingle().Which;
        diagnostic.Location.IsInSource.Should().BeTrue();
    }

    protected override IIncrementalGenerator CreateGenerator() => new Atb.Oracle.SrcGen();

    private static void AssertSqlBindNames(
                GeneratorDriver driver, string sqlName, string propertyName, MethodDeclarationSyntax method, params string[] expectedNames)
    {
        var property = CSharpSyntaxTree.ParseText(Source(driver, sqlName), cancellationToken: TestContext.Current.CancellationToken)
            .GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<PropertyDeclarationSyntax>().Should().ContainSingle(candidate => candidate.Identifier.ValueText == propertyName).Which;
        var sql = property.Initializer!.Value.Should().BeOfType<LiteralExpressionSyntax>().Which.Token.ValueText;
        var binds = SqlBindRegex.Matches(sql)
            .Select(match => match.Groups["bind"].Value).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var parameters = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => string.Equals(invocation.Expression.ToString(), "parameters.Add", StringComparison.Ordinal))
            .Select(invocation => invocation.ArgumentList.Arguments[0].Expression.Should().BeOfType<LiteralExpressionSyntax>().Which.Token.ValueText)
            .Order(StringComparer.Ordinal).ToArray();
        binds.Should().Equal(expectedNames.Order(StringComparer.Ordinal));
        parameters.Should().Equal(binds);
    }
}
