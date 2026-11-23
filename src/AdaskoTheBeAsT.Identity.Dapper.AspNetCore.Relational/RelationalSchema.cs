using System.Globalization;
using System.Text.Json;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Claims;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.Relational;

internal static class RelationalSchema
{
    private static readonly string[] RowTables = ["refresh", "mfa", "revocations", "kinds", "claims"];

    private static readonly (string Table, string Columns, string Suffix)[] Indexes =
    [
        (Table: "refresh", Columns: "scope_key", Suffix: "r_scope"),
        (Table: "refresh", Columns: "owner_key", Suffix: "r_owner"),
        (Table: "refresh", Columns: "expiry, row_key", Suffix: "r_exp"),
        (Table: "mfa", Columns: "expiry, row_key", Suffix: "m_exp"),
        (Table: "revocations", Columns: "expiry, row_key", Suffix: "v_exp"),
        (Table: "kinds", Columns: "ordinal", Suffix: "k_ord"),
        (Table: "claims", Columns: "ordinal", Suffix: "c_ord"),
        (Table: "claims", Columns: "scope_key, ordinal", Suffix: "c_kind"),
    ];

    public static async Task EnsureAsync(RelationalDatabase database, CancellationToken ct)
    {
        _ = await database.ReadAsync(
            async session =>
            {
                foreach (var statement in Statements(database))
                {
                    await session.ExecuteAsync(statement).ConfigureAwait(false);
                }

                return true;
            },
            ct).ConfigureAwait(false);
    }

    public static IReadOnlyList<string> Statements(RelationalDatabase database)
    {
        var dialect = database.Dialect;
        var conditional = dialect.IsOracle ? string.Empty : " IF NOT EXISTS";
        var number = dialect.IsOracle ? "NUMBER(19)" : "BIGINT";
        var text = dialect.IsOracle ? "VARCHAR2" : "VARCHAR";
        var payload = dialect.PayloadType;
        var transactional = dialect.IsMySql ? " ENGINE=InnoDB" : string.Empty;
        var locks = database.Table("locks");
        var statements = new List<string>
        {
            $"CREATE TABLE{conditional} {locks} (bucket {text}(16) NOT NULL PRIMARY KEY, stamp INTEGER NOT NULL){transactional}",
        };

        foreach (var name in RowTables)
        {
            var table = database.Table(name);
            var inlineIndexes = dialect.IsMySql
                ? string.Concat(Indexes.Where(index => string.Equals(index.Table, name, StringComparison.Ordinal))
                    .Select(index => $", INDEX {dialect.Quote(database.Options.TablePrefix + "_" + index.Suffix)} ({index.Columns})"))
                : string.Empty;
            statements.Add($"""
                CREATE TABLE{conditional} {table} (
                    row_key {text}(64) NOT NULL PRIMARY KEY,
                    payload {payload} NOT NULL,
                    expiry {number} NOT NULL,
                    scope_key {text}(64),
                    owner_key {text}(64),
                    ordinal INTEGER NOT NULL,
                    revoked {number},
                    reason {text}(128){inlineIndexes}
                ){transactional}
                """);
        }

        if (!dialect.IsMySql)
        {
            AddIndexes(database, statements);
        }

        if (dialect.IsOracle)
        {
            for (var i = 0; i < statements.Count; i++)
            {
                statements[i] = $"BEGIN EXECUTE IMMEDIATE '{statements[i]}'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;";
            }
        }

        foreach (var name in new[] { "refresh", "mfa", "revocations", "catalog" })
        {
            statements.Add(SeedLock(dialect, locks, name));
        }

        var kinds = database.Table("kinds");
        statements.Add(SeedKind(dialect, kinds, new ClaimKindRecord(1, IdentityClaimKindNames.FrontendClaim, null, true, true, true)));
        statements.Add(SeedKind(dialect, kinds, new ClaimKindRecord(2, IdentityClaimKindNames.BackendClaim, null, false, true, true)));
        statements.Add(SeedKind(dialect, kinds, new ClaimKindRecord(3, IdentityClaimKindNames.DataClaim, null, false, true, true)));
        return statements;
    }

    private static void AddIndexes(RelationalDatabase database, List<string> statements)
    {
        var dialect = database.Dialect;
        var conditional = dialect.IsOracle ? string.Empty : " IF NOT EXISTS";
        foreach (var index in Indexes)
        {
            var name = database.Options.TablePrefix + "_" + index.Suffix;
            var schema = database.Options.SchemaName;
            var table = dialect.IsSqlite ? dialect.Quote(database.Options.TablePrefix + "_" + index.Table) : database.Table(index.Table);
            var indexName = dialect.IsSqlite || dialect.IsOracle ? dialect.Table(schema, name) : dialect.Quote(name);
            statements.Add($"CREATE INDEX{conditional} {indexName} ON {table} ({index.Columns})");
        }
    }

    private static string SeedKind(SqlDialect dialect, string table, ClaimKindRecord record)
    {
        var key = RelationalRows.Key(record.Name.ToUpperInvariant());
        var json = JsonSerializer.Serialize(record);
        var values = string.Create(CultureInfo.InvariantCulture, $"'{key}', '{json}', {long.MaxValue}, {record.Id}");
        if (dialect.IsOracle)
        {
            return $"MERGE INTO {table} t USING (SELECT '{key}' row_key FROM dual) s ON (t.row_key = s.row_key) WHEN NOT MATCHED THEN INSERT (row_key, payload, expiry, ordinal) VALUES ({values})";
        }

        var insert = $"INSERT INTO {table} (row_key, payload, expiry, ordinal) VALUES ({values})";
        return dialect.IsMySql ? insert + " ON DUPLICATE KEY UPDATE row_key = row_key" : insert + " ON CONFLICT (row_key) DO NOTHING";
    }

    private static string SeedLock(SqlDialect dialect, string table, string name)
    {
        if (dialect.IsOracle)
        {
            return $"MERGE INTO {table} t USING (SELECT '{name}' bucket FROM dual) s ON (t.bucket = s.bucket) WHEN NOT MATCHED THEN INSERT (bucket, stamp) VALUES (s.bucket, 0)";
        }

        var insert = $"INSERT INTO {table} (bucket, stamp) VALUES ('{name}', 0)";
        return dialect.IsMySql ? insert + " ON DUPLICATE KEY UPDATE stamp = stamp" : insert + " ON CONFLICT (bucket) DO NOTHING";
    }
}
