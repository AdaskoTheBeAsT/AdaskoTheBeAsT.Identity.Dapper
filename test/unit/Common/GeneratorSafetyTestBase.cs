using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.Testing;

public abstract class GeneratorSafetyTestBase
{
    public static IEnumerable<TheoryDataRow<string, bool, bool>> Configurations()
    {
        foreach (var key in new[] { "string", "int", "long", "Guid", "System.Guid", "KeyAlias" })
        {
            foreach (var normalized in new[] { false, true })
            {
                foreach (var ownId in new[] { false, true })
                {
                    yield return new TheoryDataRow<string, bool, bool>(key, normalized, ownId);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void SupportedKeysAndCustomMappingsCompile(string key, bool skipNormalized, bool ownId)
    {
        var (driver, compilation) = GeneratorCompilation.Run(Model(key, ownId), CreateGenerator(), skipNormalized);
        GeneratorCompilation.AssertCompiles(compilation);
        var sources = driver.GetRunResult().Results.SelectMany(r => r.GeneratedSources)
            .ToDictionary(s => s.HintName, s => s.SourceText.ToString(), StringComparer.Ordinal);
        var sql = sources["IdentityUserSql.g.cs"];
        sql.Should().ContainEquivalentOf("IsActive", options => options.Using(StringComparer.OrdinalIgnoreCase));
        sql.Should().ContainEquivalentOf("DisplayLabel", options => options.Using(StringComparer.OrdinalIgnoreCase));
        sql.Should().NotContain("IgnoredProperty");
        sql.Should().NotContain("ReadOnlyProperty");
        sql.Should().Contain("OriginalConcurrencyStamp");
        var enumeration = sql[sql.IndexOf("public string GetUsersSql", StringComparison.Ordinal)..];
        enumeration.Should().NotContainEquivalentOf("JOIN", options => options.Using(StringComparer.OrdinalIgnoreCase));
        sources["IdentityUserTokenSql.g.cs"].Should().Contain("OriginalValue");
        sql.Should().Contain("GetUsersPageSql");
        sql.Should().Contain("ORDER BY");
        sql.Should().Contain("PageSize");
        sources["IdentityRoleSql.g.cs"].Should().Contain("GetRolesPageSql");
        sources["IdentityUserClaimSql.g.cs"].Should().Contain("CreateBatchItemSql");
        sources["IdentityUserClaimSql.g.cs"].Should().Contain("{0}");
        foreach (var generatedSql in sources.Where(s => s.Key.EndsWith("Sql.g.cs", StringComparison.Ordinal)))
        {
            var properties = CSharpSyntaxTree.ParseText(generatedSql.Value, cancellationToken: TestContext.Current.CancellationToken).GetRoot(cancellationToken: TestContext.Current.CancellationToken)
                .DescendantNodes().OfType<PropertyDeclarationSyntax>()
                .Where(property => property.Identifier.ValueText.EndsWith("Sql", StringComparison.Ordinal));
            properties.Should().AllSatisfy(property =>
            {
                var literal = property.Initializer!.Value.Should().BeOfType<LiteralExpressionSyntax>().Which;
                literal.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken).Should().BeTrue();
                var equalsLine = property.Initializer.EqualsToken.GetLocation().GetLineSpan().StartLinePosition.Line;
                var delimiterLine = literal.GetLocation().GetLineSpan().StartLinePosition.Line;
                delimiterLine.Should().Be(equalsLine + 1);
            });
        }
    }

    [Fact]
    public void MismatchedKeysProduceLocatedDiagnostic()
    {
        var (driver, _) = GeneratorCompilation.Run(
            Model("Guid", false).Replace("IdentityRole<Guid>", "IdentityRole<int>", StringComparison.Ordinal),
            CreateGenerator());
        var diagnostic = driver.GetRunResult().Diagnostics.Where(d => string.Equals(d.Id, "ATBID102", StringComparison.Ordinal)).Should().ContainSingle().Which;
        diagnostic.Location.IsInSource.Should().BeTrue();
    }

    [Fact]
    public void UnsupportedKeysProduceLocatedDiagnostic()
    {
        var (driver, _) = GeneratorCompilation.Run(Model("short", false), CreateGenerator());
        var diagnostic = driver.GetRunResult().Diagnostics.Where(d => string.Equals(d.Id, "ATBID101", StringComparison.Ordinal)).Should().ContainSingle().Which;
        diagnostic.Location.IsInSource.Should().BeTrue();
    }

    [Theory]
    [InlineData("abstract ")]
    [InlineData("")]
    public void InheritedMappingsGenerateOneCompleteStore(string modifier)
    {
        var userTypes = $$"""
            public {{modifier}}class UserBase : IdentityUser<Guid>
            {
                [Column("Version")] public override string? ConcurrencyStamp { get; set; }
                [Column("LoginKey")] public override string? NormalizedUserName { get; set; }
                public DateTime CreatedAt { get; set; }
            }
            public class ApplicationUser : UserBase
            """;
        var model = Model("Guid", false).Replace(
            "public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser<Guid>",
            userTypes,
            StringComparison.Ordinal);
        var (driver, compilation) = GeneratorCompilation.Run(model, CreateGenerator());
        GeneratorCompilation.AssertCompiles(compilation);
        var sql = Source(driver, "IdentityUserSql.g.cs");
        sql.Should().ContainEquivalentOf("Version", options => options.Using(StringComparer.OrdinalIgnoreCase));
        sql.Should().ContainEquivalentOf("LoginKey", options => options.Using(StringComparer.OrdinalIgnoreCase));
        sql.Should().ContainEquivalentOf("CreatedAt", options => options.Using(StringComparer.OrdinalIgnoreCase));
        sql.Should().Contain("WHERE");
        sql.Should().NotContainEquivalentOf("WHERE NormalizedUserName=", options => options.Using(StringComparer.OrdinalIgnoreCase));
        sql.Should().Contain("IIdentityUserConcurrencySql");
    }

    [Fact]
    public void InsertOwnIdIsInheritedFromApplicationBase()
    {
        const string userTypes = """
            [InsertOwnId] public abstract class UserBase : IdentityUser<Guid> { }
            public class ApplicationUser : UserBase
            """;
        var model = Model("Guid", false).Replace(
            "public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser<Guid>",
            userTypes,
            StringComparison.Ordinal);
        var (driver, compilation) = GeneratorCompilation.Run(model, CreateGenerator());
        GeneratorCompilation.AssertCompiles(compilation);
        var sql = Source(driver, "IdentityUserSql.g.cs");
        var create = sql[..sql.IndexOf("public string UpdateSql", StringComparison.Ordinal)];
        (create.Contains("@Id", StringComparison.Ordinal) || create.Contains(":Id", StringComparison.Ordinal)).Should().BeTrue();
        create.Should().NotContain("randomblob");
        create.Should().NotContain("SYS_GUID");
    }

    [Fact]
    public void UnrelatedDuplicateEntitiesReportLocatedDiagnostic()
    {
        var (driver, _) = GeneratorCompilation.Run(
            Model("Guid", false) +
            """

            public class OtherUser : Microsoft.AspNetCore.Identity.IdentityUser<System.Guid> { }
            """,
            CreateGenerator());
        var diagnostic = driver.GetRunResult().Diagnostics.Where(d => string.Equals(d.Id, "ATBID103", StringComparison.Ordinal)).Should().ContainSingle().Which;
        diagnostic.Location.IsInSource.Should().BeTrue();
    }

    [Fact]
    public void AuxiliaryCustomPropertiesCompileAndClaimReplacementPreservesThem()
    {
        var model = Model("Guid", true);
        foreach (var entity in new[] { "UserClaim", "RoleClaim", "UserLogin", "UserRole", "UserToken" })
        {
            model = model.Replace(
                $"Application{entity} : Identity{entity}<Guid> {{ }}",
                $$"""Application{{entity}} : Identity{{entity}}<Guid> { public string AuditSource { get; set; } = "source"; }""",
                StringComparison.Ordinal);
        }

        var (driver, compilation) = GeneratorCompilation.Run(model, CreateGenerator());
        GeneratorCompilation.AssertCompiles(compilation);
        var claimSql = Source(driver, "IdentityUserClaimSql.g.cs");
        claimSql.Should().ContainEquivalentOf("AuditSource", options => options.Using(StringComparer.OrdinalIgnoreCase));
        var replace = claimSql[claimSql.IndexOf("public string ReplaceSql", StringComparison.Ordinal)..claimSql.IndexOf("public string CreateBatchItemSql", StringComparison.Ordinal)];
        replace.Should().Contain("UPDATE");
        replace.Should().NotContainEquivalentOf("AuditSource", options => options.Using(StringComparer.OrdinalIgnoreCase));
        replace.Should().NotContain("DELETE");
        var store = Source(driver, "ApplicationUserStore.g.cs");
        if (store.Contains("OracleDynamicParameters", StringComparison.Ordinal))
        {
            store.Should().Contain("entity.AuditSource");
            store.Should().Contain("token.AuditSource");
            Source(driver, "ApplicationRoleStore.g.cs").Should().Contain("roleClaim.AuditSource");
        }
    }

    [Fact]
    public void MappedLookupAndJoinColumnsUseEntityMetadata()
    {
        const string userProperties = """
                public string? DisplayLabel { get; set; }
                [Column("UserKey")] public override Guid Id { get; set; }
                [Column("LoginKey")] public override string? NormalizedUserName { get; set; }
                [Column("MailKey")] public override string? NormalizedEmail { get; set; }
                """;
        const string roleType = """
                ApplicationRole : IdentityRole<Guid>
                {
                    [Column("RoleKey")] public override Guid Id { get; set; }
                    [Column("RoleLookup")] public override string? NormalizedName { get; set; }
                }
                """;
        var model = Model("Guid", true)
            .Replace(
                "public string? DisplayLabel { get; set; }",
                userProperties,
                StringComparison.Ordinal)
            .Replace(
                "ApplicationRole : IdentityRole<Guid> { }",
                roleType,
                StringComparison.Ordinal);
        var (driver, compilation) = GeneratorCompilation.Run(model, CreateGenerator());
        GeneratorCompilation.AssertCompiles(compilation);
        var sql = Source(driver, "IdentityUserSql.g.cs");
        sql.Should().ContainEquivalentOf("RoleKey", options => options.Using(StringComparer.OrdinalIgnoreCase));
        sql.Should().ContainEquivalentOf("RoleLookup", options => options.Using(StringComparer.OrdinalIgnoreCase));
        sql.Should().NotContainEquivalentOf("u.Id=", options => options.Using(StringComparer.OrdinalIgnoreCase));
        var update = sql[sql.IndexOf("public string UpdateSql", StringComparison.Ordinal)..sql.IndexOf("public string DeleteSql", StringComparison.Ordinal)];
        update.Should().NotContainEquivalentOf("SET [UserKey]", options => options.Using(StringComparer.OrdinalIgnoreCase));
        sql.Should().ContainEquivalentOf("MailKey", options => options.Using(StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("string", false)]
    [InlineData("int", true)]
    [InlineData("long", true)]
    public void KeyCreationDoesNotUseInvalidOrUnrelatedIdentityExpressions(string key, bool ownId)
    {
        var (driver, compilation) = GeneratorCompilation.Run(Model(key, ownId), CreateGenerator());
        GeneratorCompilation.AssertCompiles(compilation);
        var sql = Source(driver, "IdentityUserSql.g.cs");
        sql.Should().NotContain("NEWSEQUENTIALID()");
        sql.Should().NotContain("{tableName}");
        sql.Should().NotContain("DO $$");
        if (ownId)
        {
            sql.Should().NotContain("SCOPE_IDENTITY");
            sql.Should().NotContain("LAST_INSERT_ID");
        }
    }

    [Theory]
    [InlineData("public new string? Label => \"computed\";")]
    [InlineData("public new string? Label { get; private set; }")]
    [InlineData("[NotMapped] public new string? Label => \"computed\";")]
    [InlineData("[NotMapped] public new string? Label { get; set; }")]
    [InlineData("public new string? Label;")]
    public void ExcludedDerivedMembersDoNotResurrectBaseMappings(string member)
    {
        const string userTypes = """
                public abstract class UserBase : IdentityUser<Guid>
                {
                    [Column("StoredLabel")] public string? Label { get; set; }
                }
                public class ApplicationUser : UserBase
                """;
        var model = Model("Guid", false)
            .Replace(
                "public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser<Guid>",
                userTypes,
                StringComparison.Ordinal)
            .Replace("public string? DisplayLabel { get; set; }", member, StringComparison.Ordinal);
        var (driver, compilation) = GeneratorCompilation.Run(model, CreateGenerator());
        GeneratorCompilation.AssertCompiles(compilation);
        Source(driver, "IdentityUserSql.g.cs").Should().NotContainEquivalentOf("StoredLabel", options => options.Using(StringComparer.OrdinalIgnoreCase));
        Source(driver, "IdentityUserSql.g.cs").Should().NotContainEquivalentOf("Label", options => options.Using(StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("User", "public new string? ConcurrencyStamp => \"hidden\";")]
    [InlineData("User", "public new string? ConcurrencyStamp { get; private set; }")]
    [InlineData("User", "public override string? ConcurrencyStamp => \"read-only\";")]
    [InlineData("User", "public new Guid Id;")]
    [InlineData("Role", "public new string? ConcurrencyStamp { get; set; }")]
    [InlineData("UserToken", "public new string? Value;")]
    public void HiddenIdentityPropertiesAndFieldsProduceLocatedDiagnostic(string entity, string member)
    {
        var (driver, _) = GeneratorCompilation.Run(WithIdentityMember(entity, member), CreateGenerator());
        AssertModelErrorWithoutOutput(driver, "ATBID106");
    }

    [Theory]
    [InlineData("User", "Guid", "Id")]
    [InlineData("User", "string?", "ConcurrencyStamp")]
    [InlineData("Role", "string?", "ConcurrencyStamp")]
    [InlineData("User", "string?", "UserName")]
    [InlineData("User", "string?", "SecurityStamp")]
    [InlineData("UserToken", "Guid", "UserId")]
    [InlineData("UserToken", "string", "LoginProvider")]
    [InlineData("UserToken", "string", "Name")]
    [InlineData("UserToken", "string?", "Value")]
    [InlineData("UserClaim", "string?", "ClaimType")]
    [InlineData("RoleClaim", "Guid", "RoleId")]
    [InlineData("UserLogin", "string", "ProviderKey")]
    [InlineData("UserRole", "Guid", "UserId")]
    public void RequiredIdentityPropertiesCannotBeNotMapped(string entity, string type, string property)
    {
        var model = WithIdentityMember(entity, $"[NotMapped] public override {type} {property} {{ get; set; }}");
        var (driver, _) = GeneratorCompilation.Run(model, CreateGenerator());
        AssertModelErrorWithoutOutput(driver, "ATBID105");
    }

    [Theory]
    [InlineData("User", "NormalizedUserName", false)]
    [InlineData("User", "NormalizedUserName", true)]
    [InlineData("User", "NormalizedEmail", false)]
    [InlineData("User", "NormalizedEmail", true)]
    [InlineData("Role", "NormalizedName", false)]
    [InlineData("Role", "NormalizedName", true)]
    public void NotMappedNormalizedPropertiesRespectTheConfiguredMode(string entity, string property, bool skipNormalized)
    {
        var model = WithIdentityMember(entity, $"[NotMapped] public override string? {property} {{ get; set; }}");
        var (driver, compilation) = GeneratorCompilation.Run(model, CreateGenerator(), skipNormalized);
        if (skipNormalized)
        {
            driver.GetRunResult().Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
            GeneratorCompilation.AssertCompiles(compilation);
            driver.GetRunResult().Results.SelectMany(r => r.GeneratedSources).Should().NotBeEmpty();
        }
        else
        {
            AssertModelErrorWithoutOutput(driver, "ATBID105");
        }
    }

    [Fact]
    public void InheritedNotMappedRequiredPropertyProducesNoSources()
    {
        const string userTypes = """
            public abstract class UserBase : IdentityUser<Guid>
            {
                [NotMapped] public override string? ConcurrencyStamp { get; set; }
            }
            public class ApplicationUser : UserBase
            """;
        var model = Model("Guid", false).Replace(
            "public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser<Guid>",
            userTypes,
            StringComparison.Ordinal);
        var (driver, _) = GeneratorCompilation.Run(model, CreateGenerator());
        AssertModelErrorWithoutOutput(driver, "ATBID105");
    }

    protected static string Source(GeneratorDriver driver, string name) =>
                driver.GetRunResult().Results.SelectMany(r => r.GeneratedSources).Single(s => string.Equals(s.HintName, name, StringComparison.Ordinal)).SourceText.ToString();

    protected static string Model(string key, bool ownId) => $$"""
        using System;
        using KeyAlias = System.Guid;
        using Microsoft.AspNetCore.Identity;
        using System.ComponentModel.DataAnnotations.Schema;
        using AdaskoTheBeAsT.Identity.Dapper.Attributes;
        namespace Consumer;
        {{(ownId ? "[InsertOwnId]" : string.Empty)}}
        public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser<{{key}}>
        {
            [Column("IsActive")] public bool Active { get; set; }
            public string? DisplayLabel { get; set; }
            [NotMapped] public string? IgnoredProperty { get; set; }
            public string ReadOnlyProperty => "not persisted";
        }
        {{(ownId ? "[InsertOwnId]" : string.Empty)}}
        public class ApplicationRole : IdentityRole<{{key}}> { }
        public class ApplicationUserClaim : IdentityUserClaim<{{key}}> { }
        public class ApplicationRoleClaim : IdentityRoleClaim<{{key}}> { }
        public class ApplicationUserRole : IdentityUserRole<{{key}}> { }
        public class ApplicationUserLogin : IdentityUserLogin<{{key}}> { }
        public class ApplicationUserToken : IdentityUserToken<{{key}}> { }
        """;

    protected abstract IIncrementalGenerator CreateGenerator();

    private static string WithIdentityMember(string entity, string member)
    {
        var model = Model("Guid", false);
        return string.Equals(
            entity,
            "User",
            StringComparison.Ordinal) ? model.Replace("public string? DisplayLabel { get; set; }", member, StringComparison.Ordinal)
            : model.Replace(
                $"Application{entity} : Identity{entity}<Guid> {{ }}",
                $$"""Application{{entity}} : Identity{{entity}}<Guid> { {{member}} }""",
                StringComparison.Ordinal);
    }

    private static void AssertModelErrorWithoutOutput(GeneratorDriver driver, string id)
    {
        var result = driver.GetRunResult();
        var diagnostic = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().ContainSingle().Which;
        diagnostic.Id.Should().Be(id);
        diagnostic.Location.IsInSource.Should().BeTrue();
        result.Results.SelectMany(r => r.GeneratedSources).Should().BeEmpty();
        result.Results.Should().AllSatisfy(r => r.Exception.Should().BeNull());
    }
}
