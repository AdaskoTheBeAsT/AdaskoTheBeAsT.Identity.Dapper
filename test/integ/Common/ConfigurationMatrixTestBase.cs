using System.Data.Common;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;
using AdaskoTheBeAsT.Identity.Dapper.Testing;
using AwesomeAssertions;
using Dapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.IntegrationTest.Common;

public abstract partial class ConfigurationMatrixTestBase
{
    protected abstract IIncrementalGenerator Generator { get; }

    protected abstract string Provider { get; }

    protected abstract string ConnectionType { get; }

    public static IEnumerable<TheoryDataRow<string, bool, bool>> Configurations()
    {
        foreach (var key in new[] { "string", "int", "long", "Guid" })
        {
            foreach (var skipNormalized in new[] { false, true })
            {
                foreach (var ownId in new[] { false, true })
                {
                    yield return new TheoryDataRow<string, bool, bool>(key, skipNormalized, ownId);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public async Task GeneratedStoresExecuteForSupportedConfiguration(string key, bool skipNormalized, bool ownId)
    {
        var prefix = "m" + Guid.NewGuid().ToString("N")[..8];
        var (_, compilation) = GeneratorCompilation.Run(Model(key, ownId), Generator, skipNormalized, referencesGeneratedTypes: true);
        GeneratorCompilation.AssertCompiles(compilation);

        // Give each case private tables inside its disposable fixture database.
        // Only table identifiers change; the SQL syntax and mappings remain generated.
        compilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(
            compilation.SyntaxTrees.Select(tree => CSharpSyntaxTree.ParseText(IdentityTableRegex().Replace(
                tree.ToString(), match => prefix + match.Value))))
            .WithAssemblyName("Matrix" + prefix);
        using var assemblyBytes = new MemoryStream();
        var emitted = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current.CancellationToken);
        emitted.Success.Should().BeTrue("{0}", string.Join(Environment.NewLine, emitted.Diagnostics));
        assemblyBytes.Position = 0;
        var context = new AssemblyLoadContext(prefix, isCollectible: true);
        await using var connection = Connection();
        var connectionString = connection.ConnectionString;
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var created = new List<string>();
        try
        {
            foreach (var table in Tables(key, skipNormalized, ownId))
            {
                var name = string.Equals(Provider, nameof(DatabaseProvider.MySql), StringComparison.Ordinal)
                    ? prefix + table.Name.ToLowerInvariant()
                    : prefix + table.Name;
                await connection.ExecuteAsync(new CommandDefinition(
                    $"CREATE TABLE {name} ({table.Columns})", cancellationToken: TestContext.Current.CancellationToken));
                created.Add(name);
            }

            var assembly = context.LoadFromStream(assemblyBytes);
            var run = (Task)assembly.GetType("MatrixConsumer.Scenario")!.GetMethod("Run")!
                .Invoke(null, new object[] { connectionString, skipNormalized, ownId })!;
            await run.WaitAsync(TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);
        }
        finally
        {
            try
            {
                foreach (var table in Enumerable.Reverse(created))
                {
                    await connection.ExecuteAsync($"DROP TABLE {table}");
                }
            }
            finally
            {
                context.Unload();
            }
        }
    }

    protected abstract DbConnection Connection();

    [GeneratedRegex(@"\bAspNet(Users|Roles|UserClaims|RoleClaims|UserLogins|UserRoles|UserTokens)\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex IdentityTableRegex();

#pragma warning disable S3776
    private IEnumerable<(string Name, string Columns)> Tables(string key, bool skipNormalized, bool ownId)
#pragma warning restore S3776
    {
        var text = Provider switch
        {
            nameof(DatabaseProvider.Oracle) => "VARCHAR2(256)",
            nameof(DatabaseProvider.SqlServer) => "nvarchar(256)",
            _ => "varchar(256)",
        };
        var number = string.Equals(Provider, nameof(DatabaseProvider.Oracle), StringComparison.Ordinal) ? "NUMBER(10)" : "INTEGER";
        var boolean = Provider switch
        {
            nameof(DatabaseProvider.Oracle) => "CHAR(1)",
            nameof(DatabaseProvider.PostgreSql) => "BOOLEAN",
            nameof(DatabaseProvider.SqlServer) or nameof(DatabaseProvider.MySql) => "BIT",
            _ => "INTEGER",
        };
        var date = Provider switch
        {
            nameof(DatabaseProvider.Oracle) => "TIMESTAMP",
            nameof(DatabaseProvider.PostgreSql) => "timestamp with time zone",
            nameof(DatabaseProvider.SqlServer) => "datetimeoffset",
            _ => "datetime",
        };
        var keyType = GetKeyType(key, number);
        string Id(string column, bool auxiliary = false) =>
            PrimaryKey(column, auxiliary ? number : keyType, key, ownId, auxiliary);
        string Fields(params (string Name, string Type)[] columns) =>
            string.Join(",", columns.Select(column => $"{QuoteColumnName(column.Name)} {column.Type}"));
        var user = Id("Entity key") + "," + Fields(
            ("UserName", text),
            ("Email", text),
            ("EmailConfirmed", boolean),
            ("PasswordHash", text),
            ("SecurityStamp", text),
            ("Version", text),
            ("PhoneNumber", text),
            ("PhoneNumberConfirmed", boolean),
            ("TwoFactorEnabled", boolean),
            ("LockoutEnd", date),
            ("LockoutEnabled", boolean),
            ("AccessFailedCount", number),
            ("Display label", text));
        var role = Id("Entity key") + "," + Fields(("Name", text), ("Version", text));
        if (!skipNormalized)
        {
            user += "," + Fields(("User lookup", text), ("Email lookup", text));
            role += "," + Fields(("Role lookup", text));
        }

        yield return ("AspNetUsers", user);
        yield return ("AspNetRoles", role);
        yield return ("AspNetUserClaims", Id(nameof(Id), true) + "," + Fields(("UserId", keyType), ("ClaimType", text), ("ClaimValue", text), ("Optional number {0}", number)));
        yield return ("AspNetRoleClaims", Id(nameof(Id), true) + "," + Fields(("RoleId", keyType), ("ClaimType", text), ("ClaimValue", text)));
        yield return ("AspNetUserLogins", Fields(("UserId", keyType), ("LoginProvider", text), ("ProviderKey", text), ("ProviderDisplayName", text)));
        yield return ("AspNetUserRoles", Fields(("UserId", keyType), ("RoleId", keyType)));
        yield return ("AspNetUserTokens", Fields(("UserId", keyType), ("LoginProvider", text), ("Name", text), ("Value", text)));
    }

    private string QuoteColumnName(string name) => Provider switch
    {
        nameof(DatabaseProvider.Oracle) => "\"" + name.ToUpperInvariant() + "\"",
        nameof(DatabaseProvider.PostgreSql) => "\"" + name.ToLowerInvariant() + "\"",
        nameof(DatabaseProvider.MySql) => "`" + name + "`",
        _ => "[" + name + "]",
    };

    private string GetKeyType(string key, string number) => key switch
    {
        "int" => number,
        "long" => Provider switch
        {
            nameof(DatabaseProvider.Oracle) => "NUMBER(19)",
            nameof(DatabaseProvider.Sqlite) => "INTEGER",
            _ => "BIGINT",
        },
        "Guid" => Provider switch
        {
            nameof(DatabaseProvider.Oracle) => "RAW(16)",
            nameof(DatabaseProvider.PostgreSql) => "uuid",
            nameof(DatabaseProvider.SqlServer) => "uniqueidentifier",
            _ => "char(36)",
        },
        _ => Provider switch
        {
            nameof(DatabaseProvider.Oracle) => "VARCHAR2(36)",
            nameof(DatabaseProvider.SqlServer) => "nvarchar(36)",
            _ => "varchar(36)",
        },
    };

    private string PrimaryKey(string column, string type, string key, bool ownId, bool auxiliary)
    {
        var generated = auxiliary || (!ownId && key is "int" or "long");
        if (generated)
        {
            type += Provider switch
            {
                nameof(DatabaseProvider.Oracle) or nameof(DatabaseProvider.PostgreSql) => " GENERATED BY DEFAULT AS IDENTITY",
                nameof(DatabaseProvider.SqlServer) => " IDENTITY(1,1)",
                nameof(DatabaseProvider.MySql) => " AUTO_INCREMENT",
                _ => string.Empty,
            };
        }
        else if (!ownId && string.Equals(Provider, nameof(DatabaseProvider.SqlServer), StringComparison.Ordinal))
        {
            type += string.Equals(key, "Guid", StringComparison.Ordinal) ? " DEFAULT NEWSEQUENTIALID()" : " DEFAULT CONVERT(nvarchar(36),NEWID())";
        }

        return $"{QuoteColumnName(column)} {type} PRIMARY KEY";
    }

#pragma warning disable MA0051 // Method is too long
    private string Model(string key, bool ownId)
    {
        var seed = key switch { "string" => "\"provided-id\"", "Guid" => "Guid.NewGuid()", "long" => "5000000001L", _ => "1234567" };
        var secondSeed = key switch { "string" => "\"second-id\"", "Guid" => "Guid.NewGuid()", "long" => "5000000002L", _ => "1234568" };
        return $$"""
            using System;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Security.Claims;
            using System.ComponentModel.DataAnnotations.Schema;
            using Microsoft.AspNetCore.Identity;
            using AdaskoTheBeAsT.Identity.Dapper.Attributes;
            using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
            using AwesomeAssertions;
            namespace MatrixConsumer;
            {{(ownId ? "[InsertOwnId]" : string.Empty)}}
            public class ApplicationUser : IdentityUser<{{key}}>
            {
                [Column("Entity key")] public override {{key}} Id { get; set; }
                [Column("Version")] public override string? ConcurrencyStamp { get; set; }
                [Column("User lookup")] public override string? NormalizedUserName { get; set; }
                [Column("Email lookup")] public override string? NormalizedEmail { get; set; }
                [Column("Display label")] public string? DisplayLabel { get; set; }
            }
            {{(ownId ? "[InsertOwnId]" : string.Empty)}}
            public class ApplicationRole : IdentityRole<{{key}}>
            {
                [Column("Entity key")] public override {{key}} Id { get; set; }
                [Column("Version")] public override string? ConcurrencyStamp { get; set; }
                [Column("Role lookup")] public override string? NormalizedName { get; set; }
            }
            public class ApplicationUserClaim : IdentityUserClaim<{{key}}>
            {
                [Column("Optional number {0}")] public int? OptionalNumber { get; set; }
                [NotMapped] public UnmappedClaimState State { get; set; } = new();
            }
            public sealed class UnmappedClaimState { }
            public class ApplicationRoleClaim : IdentityRoleClaim<{{key}}> { }
            public class ApplicationUserLogin : IdentityUserLogin<{{key}}> { }
            public class ApplicationUserRole : IdentityUserRole<{{key}}> { }
            public class ApplicationUserToken : IdentityUserToken<{{key}}> { }
            public sealed class Provider(string connectionString) : IIdentityDbConnectionProvider<{{ConnectionType}}>
            {
                public {{ConnectionType}} Provide() => new(connectionString);
            }
            public static class Scenario
            {
                private static void Check(bool value, string message) => value.Should().BeTrue("{0}", message);
                private static void Success(IdentityResult result) => Check(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
                public static async Task Run(string connectionString, bool skipNormalized, bool ownId)
                {
                    var provider = new Provider(connectionString);
                    using var users = new ApplicationUserStore(provider);
                    using var roles = new ApplicationRoleStore(provider);
                    var user = new ApplicationUser { Id = {{seed}}, UserName = "alice", NormalizedUserName = "ALICE", Email = "alice@example.test", NormalizedEmail = "ALICE@EXAMPLE.TEST", DisplayLabel = "mapped" };
                    var expectedId = user.Id;
                    Success(await users.CreateAsync(user, CancellationToken.None));
                    Check(!Equals(user.Id, default({{key}})), "database key missing");
                    if (ownId) Check(Equals(expectedId, user.Id), "application key changed");
                    var loaded = (await users.FindByIdAsync(user.Id.ToString()!, CancellationToken.None))!;
                    Check(loaded != null, "user not found by returned id");
                    Check(loaded.DisplayLabel == "mapped" && loaded.ConcurrencyStamp == null, "mapped/null fields");
                    Check(await users.FindByNameAsync(skipNormalized ? "alice" : "ALICE", CancellationToken.None) != null, "name lookup");
                    Check(await users.FindByEmailAsync(skipNormalized ? "alice@example.test" : "ALICE@EXAMPLE.TEST", CancellationToken.None) != null, "email lookup");
                    loaded.DisplayLabel = null;
                    Success(await users.UpdateAsync(loaded, CancellationToken.None));
                    Check(!(await users.UpdateAsync(user, CancellationToken.None)).Succeeded, "stale update accepted");
                    Check(users.Users.Count(u => Equals(u.Id, user.Id)) == 1, "roleless enumeration");
                    var role = new ApplicationRole { Id = {{seed}}, Name = "readers", NormalizedName = "READERS" };
                    Success(await roles.CreateAsync(role, CancellationToken.None));
                    var userPage = await users.GetUsersPageAsync(0, 1, CancellationToken.None);
                    Check(userPage.Count == 1 && Equals(userPage[0].Id, user.Id), "mapped user paging");
                    Check(userPage[0].DisplayLabel == null, "paged custom null materialization");
                    Check((await users.GetUsersPageAsync(1, 1, CancellationToken.None)).Count == 0, "user offset");
                    var rolePage = await roles.GetRolesPageAsync(0, 1, CancellationToken.None);
                    Check(rolePage.Count == 1 && Equals(rolePage[0].Id, role.Id), "mapped role paging");
                    Check((await roles.GetRolesPageAsync(1, 1, CancellationToken.None)).Count == 0, "role offset");
                    var secondUser = new ApplicationUser { Id = {{secondSeed}}, UserName = "second", NormalizedUserName = "SECOND" };
                    Success(await users.CreateAsync(secondUser, CancellationToken.None));
                    var firstPage = await users.GetUsersPageAsync(0, 1, CancellationToken.None);
                    var secondPage = await users.GetUsersPageAsync(1, 1, CancellationToken.None);
                    Check(firstPage.Count == 1 && secondPage.Count == 1 && !Equals(firstPage[0].Id, secondPage[0].Id), "distinct database pages");
                    Check((await users.GetUsersPageAsync(2, 1, CancellationToken.None)).Count == 0, "end of pages");
                    Success(await users.DeleteAsync(secondUser, CancellationToken.None));
                    await roles.AddClaimAsync(role, new Claim("role", "value"), CancellationToken.None);
                    await users.AddClaimsAsync(loaded, new[] { new Claim("old", "value") }, CancellationToken.None);
                    await users.ReplaceClaimAsync(loaded, new Claim("old", "value"), new Claim("new", "value"), CancellationToken.None);
                    Check((await users.GetUsersForClaimAsync(new Claim("new", "value"), CancellationToken.None)).Count == 1, "claim join");
                    var roleName = skipNormalized ? "readers" : "READERS";
                    await users.AddToRoleAsync(loaded, roleName, CancellationToken.None);
                    Check(await users.IsInRoleAsync(loaded, roleName, CancellationToken.None), "membership");
                    Check((await users.GetUsersInRoleAsync(roleName, CancellationToken.None)).Count == 1, "role join");
                    Check((await users.GetUserAndRoleClaimsAsync(loaded, CancellationToken.None)).Count == 2, "claim union");
                    await users.AddLoginAsync(loaded, new UserLoginInfo("provider", "key", "name"), CancellationToken.None);
                    Check(await users.FindByLoginAsync("provider", "key", CancellationToken.None) != null, "login lookup");
                    await users.SetTokenAsync(loaded, "provider", "name", null, CancellationToken.None);
                    await users.SetTokenAsync(loaded, "provider", "name", "value", CancellationToken.None);
                    Check(await users.GetTokenAsync(loaded, "provider", "name", CancellationToken.None) == "value", "token update");
                    await users.RemoveTokenAsync(loaded, "provider", "name", CancellationToken.None);
                    await users.RemoveLoginAsync(loaded, "provider", "key", CancellationToken.None);
                    await users.RemoveFromRoleAsync(loaded, roleName, CancellationToken.None);
                    Success(await users.DeleteAsync(loaded, CancellationToken.None));
                    Success(await roles.DeleteAsync(role, CancellationToken.None));
                }
            }
            """;
    }
#pragma warning restore MA0051
}
