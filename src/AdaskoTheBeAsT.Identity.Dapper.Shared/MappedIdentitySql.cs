using System;
using System.Collections.Generic;
using System.Linq;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator;

// Keep predicates, joins and projections on the same mapping metadata.
internal sealed class MappedIdentitySql
{
    private const string UserEntity = "IdentityUser";
    private const string RoleEntity = "IdentityRole";
    private const string UserClaimEntity = "IdentityUserClaim";
    private const string RoleClaimEntity = "IdentityRoleClaim";
    private const string UserRoleEntity = "IdentityUserRole";
    private const string UserId = "UserId";
    private const string RoleId = "RoleId";
    private const string ClaimType = "ClaimType";
    private const string ClaimValue = "ClaimValue";
    private const string LoginProvider = "LoginProvider";
    private const string Value = "Value";
    private const string NormalizedName = "NormalizedName";
    private const string AssignmentSeparator = "\r\n   ,";

    private readonly IdentityDapperConfiguration _config;
    private readonly string _entity;
    private readonly IList<PropertyColumnTypeTriple> _properties;
    private readonly Func<string, string> _quoteColumn;
    private readonly string _parameterPrefix;

    public MappedIdentitySql(
        IdentityDapperConfiguration config,
        IList<PropertyColumnTypeTriple> properties,
        Func<string, string> quoteColumn,
        string parameterPrefix)
    {
        _config = config;
        _entity = config.BaseTypeName;
        _properties = properties;
        _quoteColumn = quoteColumn;
        _parameterPrefix = parameterPrefix;
    }

    private string ClaimOwner => string.Equals(_entity, UserClaimEntity, StringComparison.Ordinal) ? UserId : RoleId;

    private string LoginKey => And(Equal(C(LoginProvider), LoginProvider), Equal(C("ProviderKey"), "ProviderKey"));

    private string TokenKey => And(Equal(C(UserId), UserId), Equal(C(LoginProvider), LoginProvider), Equal(C("Name"), "Name"));

    private string UserRoleKey => And(Equal(C(UserId), UserId), Equal(C(RoleId), RoleId));

    public string Create()
    {
        var hasId = _entity is UserEntity or RoleEntity;
        var properties = _properties.Where(p => !Skip(p.PropertyName) &&
            (!hasId || _config.InsertOwnId || !string.Equals(p.PropertyName, "Id", StringComparison.Ordinal))).Select(p => p.PropertyName).ToList();
        var columns = properties.ConvertAll(p => C(p));
        var values = properties.ConvertAll(P);
        var prefix = string.Empty;
        var generatedGuid = hasId && !_config.InsertOwnId && _config.KeyTypeName is "Guid" or "string";

        // SQL Server uses the Id column default (NEWSEQUENTIALID in the GUID database projects).
        // String-key tables also need an Id default unless InsertOwnId is enabled.
        if (generatedGuid && _config.Provider != DatabaseProvider.SqlServer)
        {
            columns.Insert(0, C("Id"));
            if (_config.Provider == DatabaseProvider.MySql)
            {
                prefix = "SET @NewId=UUID();\r\n";
            }

            values.Insert(0, GeneratedGuidExpression());
        }

        var output = hasId && _config.Provider == DatabaseProvider.SqlServer
            ? $"OUTPUT inserted.{C("Id")}\r\n" : string.Empty;
        var insert = $$"""
            INSERT INTO {{Table()}}(
                {{string.Join(AssignmentSeparator, columns)}})
            {{output}}VALUES(
                {{string.Join(AssignmentSeparator, values)}})
            """;
        if (!hasId)
        {
            return insert + ";";
        }

        return CompleteEntityInsert(insert, prefix, generatedGuid);
    }

    // User/role concurrency is appended by the public Generate pipeline after the
    // Process hook, including when a downstream subclass supplies that hook.
    public string UpdateEntity() => Update(
        _properties.Where(p => !string.Equals(p.PropertyName, "Id", StringComparison.Ordinal) && !Skip(p.PropertyName))
            .Select(p => Equal(C(p.PropertyName), p.PropertyName)),
        Equal(C("Id"), "Id"));

    public string DeleteEntity() => Delete(_entity switch
    {
        UserEntity or RoleEntity => Equal(C("Id"), "Id"),
        UserClaimEntity or RoleClaimEntity =>
            And(Equal(C(ClaimOwner), ClaimOwner), Equal(C(ClaimType), ClaimType), Equal(C(ClaimValue), ClaimValue)),
        "IdentityUserLogin" => And(Equal(C(UserId), UserId), LoginKey),
        "IdentityUserToken" => TokenKey,
        UserRoleEntity => UserRoleKey,
        _ => throw new NotSupportedException(_entity),
    });

    public string FindById() => Select(Equal(C("Id"), "Id"), includeId: true);

    public string FindByName()
    {
        var name = string.Equals(_entity, UserEntity, StringComparison.Ordinal) ? "UserName" : "Name";
        return Select(Equal(C(_config.SkipNormalized ? name : "Normalized" + name), "Normalized" + name), includeId: true);
    }

    public string FindByEmail() =>
        Select(Equal(C(_config.SkipNormalized ? "Email" : "NormalizedEmail"), "NormalizedEmail"), includeId: true);

    public string GetAll() => Select(includeId: true);

    public string GetPage()
    {
        var query = Select(includeId: true).TrimEnd(';') + "\r\nORDER BY " + C("Id");
        return _config.Provider switch
        {
            DatabaseProvider.SqlServer =>
                query + $"\r\nOFFSET {P("Offset")} ROWS FETCH NEXT {P("PageSize")} ROWS ONLY;",
            DatabaseProvider.Oracle =>
                query + $"\r\nOFFSET {P("Offset")} ROWS FETCH NEXT {P("PageSize")} ROWS ONLY",
            _ => query + $"\r\nLIMIT {P("PageSize")} OFFSET {P("Offset")};",
        };
    }

    public string CreateClaimBatchItem()
    {
        var names = _properties.Where(p => !Skip(p.PropertyName)).Select(p => p.PropertyName).ToArray();
        return $$"""
            INSERT INTO {{FormatLiteral(Table())}}(
                {{FormatLiteral(string.Join(AssignmentSeparator, names.Select(p => C(p))))}})
            VALUES(
                {{string.Join(AssignmentSeparator, names.Select(p => P(p + "_{0}")))}});
            """;
    }

    public string DeleteClaimBatchItem() => $$"""
        DELETE FROM {{FormatLiteral(Table())}}
        WHERE {{FormatLiteral(C(UserId))}}={{P("UserId_{0}")}}
          AND {{FormatLiteral(C(ClaimType))}}={{P("ClaimType_{0}")}}
          AND {{FormatLiteral(C(ClaimValue))}}={{P("ClaimValue_{0}")}};
        """;

    public string GetUsersForClaim() => $$"""
        SELECT {{Projection("u", includeId: true)}}
        FROM {{Table()}} u INNER JOIN
             {{Table(UserClaimEntity)}} c ON {{C("Id", alias: "u")}}={{C(UserId, UserClaimEntity, "c")}}
        WHERE {{Equal(C(ClaimType, UserClaimEntity, "c"), ClaimType)}}
          AND {{Equal(C(ClaimValue, UserClaimEntity, "c"), ClaimValue)}};
        """;

    public string GetUsersInRole() => $$"""
        SELECT {{Projection("u", includeId: true)}}
        FROM {{Table()}} u INNER JOIN
             {{Table(UserRoleEntity)}} ur ON {{C("Id", alias: "u")}}={{C(UserId, UserRoleEntity, "ur")}} INNER JOIN
             {{Table(RoleEntity)}} r ON {{C(RoleId, UserRoleEntity, "ur")}}={{C("Id", RoleEntity, "r")}}
        WHERE {{Equal(C(_config.SkipNormalized ? "Name" : NormalizedName, RoleEntity, "r"), NormalizedName)}};
        """;

    public string GetClaimsByOwnerId() => $$"""
        SELECT {{C(ClaimType)}} AS {{Alias("Type")}}
              ,{{C(ClaimValue)}} AS {{Alias(Value)}}
        FROM {{Table()}}
        WHERE {{Equal(C(ClaimOwner), "Id")}};
        """;

    // Preserve auxiliary claim fields and the row identity.
    public string ReplaceClaim() => Update(
        new[] { Equal(C(ClaimType), "ClaimTypeNew"), Equal(C(ClaimValue), "ClaimValueNew") },
        And(Equal(C(UserId), UserId), Equal(C(ClaimType), "ClaimTypeOld"), Equal(C(ClaimValue), "ClaimValueOld")));

    public string GetLoginByUserId() => Select(Equal(C(UserId), "Id"));

    public string GetByUserIdLoginProviderKey() => Select(And(Equal(C(UserId), UserId), LoginKey));

    public string GetByLoginProviderKey() => Select(LoginKey);

    public string UpdateToken(string tableName) =>
        Update(new[] { Equal(C(Value), Value) }, And(TokenKey, TokenValueMatches()), tableName);

    public string GetToken() => Select(TokenKey);

    public string GetByUserIdRoleId() => Select(UserRoleKey);

    public string GetCount() => $$"""
        SELECT COUNT(*)
        FROM {{Table()}}
        WHERE {{UserRoleKey}};
        """;

    public string GetRoleNamesByUserId() => $$"""
        SELECT {{C(_config.SkipNormalized ? "Name" : NormalizedName, RoleEntity, "r")}}
        FROM {{Table(RoleEntity)}} r INNER JOIN
             {{Table()}} ur ON {{C("Id", RoleEntity, "r")}}={{C(RoleId, alias: "ur")}}
        WHERE {{Equal(C(UserId, alias: "ur"), UserId)}};
        """;

    public string GetRoleClaimsByUserId() => RoleClaimsQuery(distinct: true) + ";";

    public string GetUserAndRoleClaimsByUserId() => $$"""
            SELECT {{C(ClaimType, UserClaimEntity, "uc")}} AS {{Alias("Type")}}
                  ,{{C(ClaimValue, UserClaimEntity, "uc")}} AS {{Alias(Value)}}
            FROM {{Table(UserClaimEntity)}} uc
            WHERE {{Equal(C(UserId, UserClaimEntity, "uc"), "Id")}}
            UNION
            {{RoleClaimsQuery(distinct: false)}};
            """;

    internal static string QuoteIdentifier(DatabaseProvider provider, string name) => provider switch
    {
        DatabaseProvider.MySql => "`" + name.Replace("`", "``") + "`",
        DatabaseProvider.PostgreSql => "\"" + name.ToLowerInvariant().Replace("\"", "\"\"") + "\"",
        DatabaseProvider.Oracle => "\"" + name.ToUpperInvariant().Replace("\"", "\"\"") + "\"",
        DatabaseProvider.Sqlite => "\"" + name.Replace("\"", "\"\"") + "\"",
        _ => "[" + name.Replace("]", "]]") + "]",
    };

    internal static string AppendConcurrencyPredicate(string sql, string column, string parameter) =>
        sql.TrimEnd().TrimEnd(';') +
        $"\r\n  AND ({column}={parameter} OR ({column} IS NULL AND {parameter} IS NULL));";

    private static string And(params string[] predicates) => string.Join("\r\n  AND ", predicates);

    private static string FormatLiteral(string value) => value.Replace("{", "{{").Replace("}", "}}");

    private string P(string name) => _parameterPrefix + name;

    private string Q(string name) => _quoteColumn(name);

    private string Alias(string name) => _config.Provider == DatabaseProvider.PostgreSql
        ? "\"" + name.Replace("\"", "\"\"") + "\"" : Q(name);

    private string C(string property, string? entity = null, string? alias = null) =>
        (alias == null ? string.Empty : alias + ".") + Q(_config.Column(entity ?? _entity, property));

    private string Table(string? entity = null)
    {
        var name = "AspNet" + (entity ?? _entity).Substring(nameof(Identity).Length) + "s";
        if (_config.Provider is DatabaseProvider.MySql or DatabaseProvider.PostgreSql)
        {
            name = name.ToLowerInvariant();
        }

        return _config.SchemaPart + name;
    }

    private bool Skip(string property) => _config.SkipNormalized &&
        property is "NormalizedUserName" or "NormalizedEmail" or NormalizedName;

    private string ReadColumn(string property, string? alias) =>
        C(Skip(property) ? property.Substring("Normalized".Length) : property, alias: alias);

    private string Projection(string? alias = null, bool includeId = false)
    {
        var names = _properties.Select(p => p.PropertyName).ToList();
        if (includeId && !names.Contains("Id"))
        {
            names.Insert(0, "Id");
        }

        return string.Join("\r\n      ,", names.Select(p => $"{ReadColumn(p, alias)} AS {Alias(p)}"));
    }

    private string Select(string? predicate = null, string? alias = null, bool includeId = false) =>
        $$"""
        SELECT {{Projection(alias, includeId)}}
        FROM {{Table()}}
        """ + (alias == null ? string.Empty : " " + alias) +
        (predicate == null ? string.Empty : "\r\nWHERE " + predicate) + ";";

    private string Equal(string column, string parameter) => $"{column}={P(parameter)}";

    private string Delete(string predicate) =>
        $$"""
        DELETE FROM {{Table()}}
        WHERE {{predicate}};
        """;

    private string Update(IEnumerable<string> assignments, string predicate, string? tableName = null) =>
        $$"""
        UPDATE {{(tableName == null ? Table() : _config.SchemaPart + tableName)}}
        SET {{string.Join(AssignmentSeparator, assignments)}}
        WHERE {{predicate}};
        """;

    private string TokenValueMatches()
    {
        var column = C(Value);
        var parameter = P("OriginalValue");
        var equality = _config.Provider switch
        {
            // Binary collation alone still ignores trailing spaces in SQL Server.
            DatabaseProvider.SqlServer => $"CONVERT(varbinary(max),{column})=CONVERT(varbinary(max),{parameter})",
            DatabaseProvider.MySql => $"CAST({column} AS BINARY)=CAST({parameter} AS BINARY)",
            DatabaseProvider.PostgreSql => $"convert_to({column},'UTF8')=convert_to({parameter},'UTF8')",
            DatabaseProvider.Sqlite => $"CAST({column} AS BLOB)=CAST({parameter} AS BLOB)",
            DatabaseProvider.Oracle => $"UTL_RAW.CAST_TO_RAW({column})=UTL_RAW.CAST_TO_RAW({parameter})",
            _ => throw new NotSupportedException(),
        };
        return $"({equality} OR ({column} IS NULL AND {parameter} IS NULL))";
    }

    private string GeneratedGuidExpression() => _config.Provider switch
    {
        DatabaseProvider.PostgreSql => string.Equals(_config.KeyTypeName, "Guid", StringComparison.Ordinal) ? "gen_random_uuid()" : "gen_random_uuid()::text",
        DatabaseProvider.MySql => "@NewId",
        DatabaseProvider.Oracle => string.Equals(_config.KeyTypeName, "Guid", StringComparison.Ordinal) ? "SYS_GUID()" : "RAWTOHEX(SYS_GUID())",
        DatabaseProvider.Sqlite => "lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || '4' || substr(hex(randomblob(2)), 2) || '-' || substr('89ab', 1 + (abs(random()) % 4), 1) || substr(hex(randomblob(2)), 2) || '-' || hex(randomblob(6)))",
        _ => throw new NotSupportedException(),
    };

    private string CompleteEntityInsert(string insert, string prefix, bool generatedGuid)
    {
        var mysqlId = generatedGuid ? "@NewId" : "LAST_INSERT_ID()";
        if (_config.InsertOwnId)
        {
            mysqlId = "@Id";
        }

        return _config.Provider switch
        {
            DatabaseProvider.SqlServer => insert + ";",
            DatabaseProvider.PostgreSql or DatabaseProvider.Sqlite => $$"""
                {{insert}}
                RETURNING {{C("Id")}} AS {{Alias("Id")}};
                """,
            DatabaseProvider.Oracle => $$"""
                BEGIN
                {{insert}}
                RETURNING {{C("Id")}} INTO :OutputId;
                END;
                """,
            DatabaseProvider.MySql => prefix + insert + ";\r\nSELECT " +
                mysqlId + " AS Id;",
            _ => throw new NotSupportedException(),
        };
    }

    private string RoleClaimsQuery(bool distinct)
    {
        var select = distinct ? "SELECT DISTINCT" : "SELECT";
        return $$"""
            {{select}} {{C(ClaimType, RoleClaimEntity, "rc")}} AS {{Alias("Type")}}
            {{new string(' ', select.Length)}},{{C(ClaimValue, RoleClaimEntity, "rc")}} AS {{Alias(Value)}}
            FROM {{Table(RoleClaimEntity)}} rc INNER JOIN
                 {{Table(UserRoleEntity)}} ur ON {{C(RoleId, UserRoleEntity, "ur")}}={{C(RoleId, RoleClaimEntity, "rc")}}
            WHERE {{Equal(C(UserId, UserRoleEntity, "ur"), "Id")}}
            """;
    }
}
