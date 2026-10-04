using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace AdaskoTheBeAsT.Identity.Dapper;

/// <summary>Materializes database nulls without changing process-wide Dapper settings.</summary>
public static class IdentityQuery
{
    private static readonly MethodInfo IsDBNullMethod =
        typeof(IDataRecord)
            .GetMethod(
                nameof(IDataRecord.IsDBNull),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                binder: null,
                types: [typeof(int)],
                modifiers: null)!;

    private static readonly ConditionalWeakTable<Type, object> ParserCaches = new();

    public static IEnumerable<T> QueryIdentity<T>(this IDbConnection connection, string sql)
        where T : class
    {
        using var reader = connection.ExecuteReader(sql);
        var parse = CreateParser<T>(reader);
        var rows = new List<T>();
        while (reader.Read())
        {
            rows.Add(parse(reader));
        }

        return rows;
    }

    public static async Task<IEnumerable<T>> QueryIdentityAsync<T>(
        this IDbConnection connection, string sql, object? parameters = null, CancellationToken cancellationToken = default)
        where T : class
    {
        using var reader = await connection.ExecuteReaderAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
        var parse = CreateParser<T>(reader);
        var rows = new List<T>();
        while (await ReadAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            rows.Add(parse(reader));
        }

        return rows;
    }

    public static async Task<T?> QueryIdentityFirstOrDefaultAsync<T>(
        this IDbConnection connection, string sql, object? parameters = null, CancellationToken cancellationToken = default)
        where T : class
    {
        using var reader = await connection.ExecuteReaderAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
        var parse = CreateParser<T>(reader);
        return await ReadAsync(reader, cancellationToken).ConfigureAwait(false) ? parse(reader) : null;
    }

    private static Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return reader is DbDataReader asyncReader ? asyncReader.ReadAsync(cancellationToken) : Task.FromResult(reader.Read());
    }

    private static Func<IDataReader, T> CreateParser<T>(IDataReader reader)
        where T : class
        => ((ParserCache<T>)ParserCaches.GetValue(typeof(T), static _ => new ParserCache<T>())).GetParser(reader);

    private static Func<IDataReader, T> BuildParser<T>(IDataReader reader, SqlMapper.ITypeMap map)
        where T : class
    {
        var parse = reader.GetRowParser<T>();
        var value = Expression.Parameter(typeof(T), "value");
        var row = Expression.Parameter(typeof(IDataReader), "row");
        var assignments = new List<Expression>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var property = map.GetMember(reader.GetName(i))?.Property;
            if (property?.SetMethod is { } setter &&
                (!property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null))
            {
                // Use the actual mapped setter, including non-public, inherited and virtual setters.
                assignments.Add(Expression.IfThen(
                    Expression.Call(row, IsDBNullMethod, Expression.Constant(i)),
                    Expression.Call(
                        Expression.Convert(value, setter.DeclaringType!),
                        setter,
                        Expression.Default(property.PropertyType))));
            }
        }

        if (assignments.Count == 0)
        {
            return parse;
        }

        // Compile once per cached schema; the row path contains no reflection or boxing of nullables.
        var assignNulls = Expression.Lambda<Action<T, IDataReader>>(
            Expression.Block(assignments), value, row).Compile();
        return record =>
        {
            var result = parse(record);

            // Dapper normally skips DBNull assignments, leaving constructor defaults intact.
            assignNulls(result, record);
            return result;
        };
    }

    private sealed class ParserCache<T>
        where T : class
    {
        // FIFO bounds all schema metadata and delegates retained by this cache for each entity type.
        private const int Capacity = 64;
        private readonly object _sync = new();
        private readonly CacheEntry?[] _entries = new CacheEntry?[Capacity];
        private SqlMapper.ITypeMap? _map;
        private int _count;
        private int _next;

        public Func<IDataReader, T> GetParser(IDataReader reader)
        {
            lock (_sync)
            {
                while (true)
                {
                    var map = SqlMapper.GetTypeMap(typeof(T));
                    if (!ReferenceEquals(map, _map))
                    {
                        // SetTypeMap purges Dapper's own parsers; discard our corresponding plans too.
                        Array.Clear(_entries, 0, _entries.Length);
                        _count = 0;
                        _next = 0;
                        _map = map;
                    }

                    var hash = GetSchemaHash(reader);
                    for (var i = 0; i < _count; i++)
                    {
                        var entry = _entries[i]!;
                        if (entry.Hash == hash && entry.Matches(reader))
                        {
                            return entry.Parse;
                        }
                    }

                    var parse = BuildParser<T>(reader, map);
                    if (!ReferenceEquals(map, SqlMapper.GetTypeMap(typeof(T))))
                    {
                        // Do not publish a plan if its type map changed while it was being built.
                        continue;
                    }

                    _entries[_next] = new CacheEntry(reader, hash, parse);
                    _next = (_next + 1) % Capacity;
                    if (_count < Capacity)
                    {
                        _count++;
                    }

                    return parse;
                }
            }
        }

        private static int GetSchemaHash(IDataReader reader)
        {
            unchecked
            {
                var hash = reader.FieldCount;
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(reader.GetName(i));
                    hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(reader.GetFieldType(i));
                }

                return hash;
            }
        }

        private sealed class CacheEntry
        {
            private readonly string[] _names;
            private readonly Type[] _types;

            public CacheEntry(IDataReader reader, int hash, Func<IDataReader, T> parse)
            {
                Hash = hash;
                Parse = parse;
                _names = new string[reader.FieldCount];
                _types = new Type[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    _names[i] = reader.GetName(i);
                    _types[i] = reader.GetFieldType(i);
                }
            }

            public int Hash { get; }

            public Func<IDataReader, T> Parse { get; }

            public bool Matches(IDataReader reader)
            {
                if (_names.Length != reader.FieldCount)
                {
                    return false;
                }

                for (var i = 0; i < _names.Length; i++)
                {
                    // Do not fold case: custom Dapper maps may distinguish differently cased names.
                    if (!string.Equals(_names[i], reader.GetName(i), StringComparison.Ordinal) ||
                        _types[i] != reader.GetFieldType(i))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
