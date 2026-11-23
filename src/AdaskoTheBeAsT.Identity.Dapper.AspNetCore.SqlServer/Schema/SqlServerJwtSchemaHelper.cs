using System;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Internal;
using AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Options;

namespace AdaskoTheBeAsT.Identity.Dapper.AspNetCore.SqlServer.Schema;

public static class SqlServerJwtSchemaHelper
{
    public static string BuildCreateSchemaAndTablesSql(IdentityJwtSqlServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var schemaRaw = options.SchemaName;
        var refreshRaw = options.RefreshTokensTableName;
        var mfaRaw = options.MfaChallengesTableName;
        var revRaw = options.RevocationsTableName;
        var claimKindsRaw = options.ClaimKindsTableName;
        var claimsRaw = options.ClaimsTableName;

        var quotedSchema = SqlIdentifierValidator.QuoteIdentifier(schemaRaw, nameof(options.SchemaName));
        var refreshTable = SqlIdentifierValidator.QuoteQualifiedTable(schemaRaw, refreshRaw);
        var mfaTable = SqlIdentifierValidator.QuoteQualifiedTable(schemaRaw, mfaRaw);
        var revTable = SqlIdentifierValidator.QuoteQualifiedTable(schemaRaw, revRaw);
        var claimKindsTable = SqlIdentifierValidator.QuoteQualifiedTable(schemaRaw, claimKindsRaw);
        var claimsTable = SqlIdentifierValidator.QuoteQualifiedTable(schemaRaw, claimsRaw);

        return string.Join(
            Environment.NewLine,
            BuildSchemaHeaderSql(schemaRaw, quotedSchema),
            BuildRefreshTokensTableSql(schemaRaw, refreshRaw, refreshTable),
            BuildMfaChallengesTableSql(schemaRaw, mfaRaw, mfaTable),
            BuildRevocationsTableSql(schemaRaw, revRaw, revTable),
            BuildClaimKindsTableSql(schemaRaw, claimKindsRaw, claimKindsTable),
            BuildClaimsTableSql(schemaRaw, claimsRaw, claimsTable, claimKindsRaw, claimKindsTable));
    }

    private static string BuildSchemaHeaderSql(
        string schemaRaw,
        string quotedSchema) =>
        $"""
        IF SCHEMA_ID(N'{schemaRaw}') IS NULL
            EXEC(N'CREATE SCHEMA {quotedSchema}');
        """;

    private static string BuildRefreshTokensTableSql(
        string schemaRaw,
        string tableRaw,
        string qualifiedTable) =>
        $"""
        IF OBJECT_ID(N'[{schemaRaw}].[{tableRaw}]', N'U') IS NULL
        BEGIN
            CREATE TABLE {qualifiedTable} (
                [Id]                    UNIQUEIDENTIFIER   NOT NULL CONSTRAINT [PK_{tableRaw}] PRIMARY KEY NONCLUSTERED,
                [TokenHash]             CHAR(64)           NOT NULL,
                [HashKeyId]             NVARCHAR(64)       NOT NULL,
                [SessionId]             NVARCHAR(64)       NOT NULL,
                [UserId]                NVARCHAR(450)      NOT NULL,
                [SecurityStamp]         NVARCHAR(256)      NULL,
                [AuthenticationMethods] NVARCHAR(128)      NOT NULL,
                [IssuedAtUtc]           DATETIMEOFFSET(7)  NOT NULL,
                [ExpiresAtUtc]          DATETIMEOFFSET(7)  NOT NULL,
                [SessionExpiresAtUtc]   DATETIMEOFFSET(7)  NOT NULL,
                [ConsumedAtUtc]         DATETIMEOFFSET(7)  NULL,
                [ReplacedByTokenHash]   CHAR(64)           NULL,
                [RevokedAtUtc]          DATETIMEOFFSET(7)  NULL,
                [RevocationReason]      NVARCHAR(128)      NULL
            );
            CREATE UNIQUE CLUSTERED INDEX [IX_{tableRaw}_TokenHash] ON {qualifiedTable} ([TokenHash] ASC);
            CREATE NONCLUSTERED INDEX [IX_{tableRaw}_SessionId] ON {qualifiedTable} ([SessionId] ASC);
            CREATE NONCLUSTERED INDEX [IX_{tableRaw}_UserId_Active] ON {qualifiedTable} ([UserId] ASC) INCLUDE ([SessionId], [ExpiresAtUtc]) WHERE ([RevokedAtUtc] IS NULL AND [ConsumedAtUtc] IS NULL);
            CREATE NONCLUSTERED INDEX [IX_{tableRaw}_ExpiresAtUtc] ON {qualifiedTable} ([ExpiresAtUtc] ASC);
        END;
        """;

    private static string BuildMfaChallengesTableSql(
        string schemaRaw,
        string tableRaw,
        string qualifiedTable) =>
        $"""
        IF OBJECT_ID(N'[{schemaRaw}].[{tableRaw}]', N'U') IS NULL
        BEGIN
            CREATE TABLE {qualifiedTable} (
                [ChallengeId]          UNIQUEIDENTIFIER   NOT NULL CONSTRAINT [PK_{tableRaw}] PRIMARY KEY NONCLUSTERED,
                [TokenHash]            CHAR(64)           NOT NULL,
                [HashKeyId]            NVARCHAR(64)       NOT NULL,
                [Purpose]              TINYINT            NOT NULL,
                [UserId]               NVARCHAR(450)      NOT NULL,
                [SecurityStamp]        NVARCHAR(256)      NULL,
                [AllowedMethods]       NVARCHAR(128)      NOT NULL,
                [SelectedMethod]       NVARCHAR(32)       NULL,
                [OtpCodeHash]          CHAR(64)           NULL,
                [AttemptCount]         INT                NOT NULL,
                [MaxAttempts]          INT                NOT NULL,
                [ResendCount]          INT                NOT NULL,
                [ResendAvailableAtUtc] DATETIMEOFFSET(7)  NULL,
                [CreatedAtUtc]         DATETIMEOFFSET(7)  NOT NULL,
                [ExpiresAtUtc]         DATETIMEOFFSET(7)  NOT NULL
            );
            CREATE UNIQUE CLUSTERED INDEX [IX_{tableRaw}_TokenHash] ON {qualifiedTable} ([TokenHash] ASC);
            CREATE NONCLUSTERED INDEX [IX_{tableRaw}_ExpiresAtUtc] ON {qualifiedTable} ([ExpiresAtUtc] ASC);
        END;
        """;

    private static string BuildRevocationsTableSql(
        string schemaRaw,
        string tableRaw,
        string qualifiedTable) =>
        $"""
        IF OBJECT_ID(N'[{schemaRaw}].[{tableRaw}]', N'U') IS NULL
        BEGIN
            CREATE TABLE {qualifiedTable} (
                [Id]               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{tableRaw}] PRIMARY KEY CLUSTERED,
                [Issuer]           NVARCHAR(256)        NOT NULL,
                [ScopeType]        TINYINT              NOT NULL,
                [SubjectKey]       NVARCHAR(450)        NOT NULL,
                [RevokedBeforeUtc] DATETIMEOFFSET(7)    NULL,
                [RevokedAtUtc]     DATETIMEOFFSET(7)    NOT NULL,
                [ExpiresAtUtc]     DATETIMEOFFSET(7)    NOT NULL,
                [Reason]           NVARCHAR(128)        NULL
            );
            CREATE UNIQUE NONCLUSTERED INDEX [IX_{tableRaw}_Lookup] ON {qualifiedTable} ([Issuer] ASC, [ScopeType] ASC, [SubjectKey] ASC) INCLUDE ([ExpiresAtUtc], [RevokedBeforeUtc]);
            CREATE NONCLUSTERED INDEX [IX_{tableRaw}_ExpiresAtUtc] ON {qualifiedTable} ([ExpiresAtUtc] ASC);
        END;
        """;

    private static string BuildClaimKindsTableSql(
        string schemaRaw,
        string tableRaw,
        string qualifiedTable) =>
        $"""
        IF OBJECT_ID(N'[{schemaRaw}].[{tableRaw}]', N'U') IS NULL
        BEGIN
            CREATE TABLE {qualifiedTable} (
                [Id]                      INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{tableRaw}] PRIMARY KEY CLUSTERED,
                [Name]                    NVARCHAR(128)     NOT NULL,
                [Description]             NVARCHAR(512)     NULL,
                [IncludeInAccessToken]    BIT               NOT NULL CONSTRAINT [DF_{tableRaw}_IncludeInAccessToken] DEFAULT (0),
                [IncludeInTransformation] BIT               NOT NULL CONSTRAINT [DF_{tableRaw}_IncludeInTransformation] DEFAULT (1),
                [IsActive]                BIT               NOT NULL CONSTRAINT [DF_{tableRaw}_IsActive] DEFAULT (1)
            );
            CREATE UNIQUE NONCLUSTERED INDEX [IX_{tableRaw}_Name] ON {qualifiedTable} ([Name] ASC);
        END;

        IF NOT EXISTS (SELECT 1 FROM {qualifiedTable} WHERE [Name] = N'FrontendClaim')
        BEGIN
            INSERT INTO {qualifiedTable} ([Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive])
            VALUES (N'FrontendClaim', N'UI route, guard, and presentation claims included in frontend JWT access tokens.', 1, 1, 1);
        END;

        IF NOT EXISTS (SELECT 1 FROM {qualifiedTable} WHERE [Name] = N'BackendClaim')
        BEGIN
            INSERT INTO {qualifiedTable} ([Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive])
            VALUES (N'BackendClaim', N'Server-side authorization and policy claims enriched during API request transformation.', 0, 1, 1);
        END;

        IF NOT EXISTS (SELECT 1 FROM {qualifiedTable} WHERE [Name] = N'DataClaim')
        BEGIN
            INSERT INTO {qualifiedTable} ([Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive])
            VALUES (N'DataClaim', N'Row-level, scope, and resource data filter claims enriched during API request transformation.', 0, 1, 1);
        END;
        """;

    private static string BuildClaimsTableSql(
        string schemaRaw,
        string tableRaw,
        string qualifiedTable,
        string claimKindsRaw,
        string qualifiedClaimKindsTable) =>
        $"""
        IF OBJECT_ID(N'[{schemaRaw}].[{tableRaw}]', N'U') IS NULL
        BEGIN
            CREATE TABLE {qualifiedTable} (
                [Id]          INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_{tableRaw}] PRIMARY KEY CLUSTERED,
                [ClaimKindId] INT               NOT NULL CONSTRAINT [FK_{tableRaw}_{claimKindsRaw}_ClaimKindId] FOREIGN KEY REFERENCES {qualifiedClaimKindsTable} ([Id]),
                [ClaimType]   NVARCHAR(256)     NOT NULL,
                [ValueType]   NVARCHAR(256)     NOT NULL CONSTRAINT [DF_{tableRaw}_ValueType] DEFAULT (N'http://www.w3.org/2001/XMLSchema#string'),
                [DisplayName] NVARCHAR(256)     NULL,
                [Description] NVARCHAR(512)     NULL,
                [IsActive]    BIT               NOT NULL CONSTRAINT [DF_{tableRaw}_IsActive] DEFAULT (1)
            );
            CREATE UNIQUE NONCLUSTERED INDEX [IX_{tableRaw}_ClaimType] ON {qualifiedTable} ([ClaimType] ASC);
            CREATE NONCLUSTERED INDEX [IX_{tableRaw}_ClaimKindId] ON {qualifiedTable} ([ClaimKindId] ASC);
        END;
        """;
}
