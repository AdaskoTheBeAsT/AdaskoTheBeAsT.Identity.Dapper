using System;
using System.Collections.Generic;
using System.Linq;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

public class IdentityDapperConfiguration
{
    public IdentityDapperConfiguration(
        string baseTypeName,
        string keyTypeName,
        string namespaceName,
        string schemaPart,
        bool skipNormalized,
        bool insertOwnId)
    {
        BaseTypeName = baseTypeName ?? throw new ArgumentNullException(nameof(baseTypeName));
        KeyTypeName = keyTypeName ?? throw new ArgumentNullException(nameof(keyTypeName));
        NamespaceName = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
        SchemaPart = schemaPart ?? throw new ArgumentNullException(nameof(schemaPart));
        SkipNormalized = skipNormalized;
        InsertOwnId = insertOwnId;
    }

    public string BaseTypeName { get; }

    public string KeyTypeName { get; }

    public string NamespaceName { get; }

    public string SchemaPart { get; }

    public bool SkipNormalized { get; }

    public bool InsertOwnId { get; }

    public DatabaseProvider Provider { get; set; }

    public IDictionary<string, IDictionary<string, string>> ColumnMappings { get; set; } =
        new Dictionary<string, IDictionary<string, string>>(StringComparer.Ordinal);

    public string Column(string entity, string property) =>
        ColumnMappings.TryGetValue(entity, out var columns) && columns.TryGetValue(property, out var column) &&
        !string.IsNullOrEmpty(column) ? column : property;

    internal IdentityDapperConfiguration ForGeneration(
        string entity,
        DatabaseProvider provider,
        IEnumerable<PropertyColumnTypeTriple> properties)
    {
        var mappings = new Dictionary<string, IDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var mapping in ColumnMappings)
        {
            mappings[mapping.Key] = new Dictionary<string, string>(mapping.Value, StringComparer.Ordinal);
        }

        if (!mappings.TryGetValue(entity, out var columns))
        {
            columns = new Dictionary<string, string>(StringComparer.Ordinal);
            mappings[entity] = columns;
        }

        // Explicit triples win; standard default triples must not erase configured mappings.
        foreach (var property in properties.Where(property =>
            !string.Equals(property.ColumnName, property.PropertyName, StringComparison.Ordinal) || !columns.ContainsKey(property.PropertyName)))
        {
            columns[property.PropertyName] = property.ColumnName;
        }

        return new IdentityDapperConfiguration(
            entity, CanonicalKeyType(KeyTypeName), NamespaceName, SchemaPart, SkipNormalized, InsertOwnId)
        {
            Provider = provider,
            ColumnMappings = mappings,
        };
    }

    private static string CanonicalKeyType(string name) => name switch
    {
        "System.Guid" or "global::System.Guid" => "Guid",
        "String" or "System.String" or "global::System.String" => "string",
        "Int32" or "System.Int32" or "global::System.Int32" => "int",
        "Int64" or "System.Int64" or "global::System.Int64" => "long",
        "UInt32" or "System.UInt32" or "global::System.UInt32" => "uint",
        "UInt64" or "System.UInt64" or "global::System.UInt64" or "USystem.Int64" => "ulong",
        _ => name,
    };
}
