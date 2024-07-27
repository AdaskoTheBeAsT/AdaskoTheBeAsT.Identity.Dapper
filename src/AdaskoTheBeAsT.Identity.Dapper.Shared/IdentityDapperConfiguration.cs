using System;

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
}
