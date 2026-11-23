IF OBJECT_ID(N'[dbo].[IdentityJwtRefreshTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IdentityJwtRefreshTokens] (
        [Id]                    UNIQUEIDENTIFIER   NOT NULL CONSTRAINT [PK_IdentityJwtRefreshTokens] PRIMARY KEY NONCLUSTERED,
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

    CREATE UNIQUE CLUSTERED INDEX [IX_IdentityJwtRefreshTokens_TokenHash]
        ON [dbo].[IdentityJwtRefreshTokens] ([TokenHash] ASC);

    CREATE NONCLUSTERED INDEX [IX_IdentityJwtRefreshTokens_SessionId]
        ON [dbo].[IdentityJwtRefreshTokens] ([SessionId] ASC);

    CREATE NONCLUSTERED INDEX [IX_IdentityJwtRefreshTokens_UserId_Active]
        ON [dbo].[IdentityJwtRefreshTokens] ([UserId] ASC)
        INCLUDE ([SessionId], [ExpiresAtUtc])
        WHERE ([RevokedAtUtc] IS NULL AND [ConsumedAtUtc] IS NULL);

    CREATE NONCLUSTERED INDEX [IX_IdentityJwtRefreshTokens_ExpiresAtUtc]
        ON [dbo].[IdentityJwtRefreshTokens] ([ExpiresAtUtc] ASC);
END;
GO

IF OBJECT_ID(N'[dbo].[IdentityJwtMfaChallenges]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IdentityJwtMfaChallenges] (
        [ChallengeId]          UNIQUEIDENTIFIER   NOT NULL CONSTRAINT [PK_IdentityJwtMfaChallenges] PRIMARY KEY NONCLUSTERED,
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

    CREATE UNIQUE CLUSTERED INDEX [IX_IdentityJwtMfaChallenges_TokenHash]
        ON [dbo].[IdentityJwtMfaChallenges] ([TokenHash] ASC);

    CREATE NONCLUSTERED INDEX [IX_IdentityJwtMfaChallenges_ExpiresAtUtc]
        ON [dbo].[IdentityJwtMfaChallenges] ([ExpiresAtUtc] ASC);
END;
GO

IF OBJECT_ID(N'[dbo].[IdentityJwtRevocations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IdentityJwtRevocations] (
        [Id]               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_IdentityJwtRevocations] PRIMARY KEY CLUSTERED,
        [Issuer]           NVARCHAR(256)        NOT NULL,
        [ScopeType]        TINYINT              NOT NULL,
        [SubjectKey]       NVARCHAR(450)        NOT NULL,
        [RevokedBeforeUtc] DATETIMEOFFSET(7)    NULL,
        [RevokedAtUtc]     DATETIMEOFFSET(7)    NOT NULL,
        [ExpiresAtUtc]     DATETIMEOFFSET(7)    NOT NULL,
        [Reason]           NVARCHAR(128)        NULL
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_IdentityJwtRevocations_Lookup]
        ON [dbo].[IdentityJwtRevocations] ([Issuer] ASC, [ScopeType] ASC, [SubjectKey] ASC)
        INCLUDE ([ExpiresAtUtc], [RevokedBeforeUtc]);

    CREATE NONCLUSTERED INDEX [IX_IdentityJwtRevocations_ExpiresAtUtc]
        ON [dbo].[IdentityJwtRevocations] ([ExpiresAtUtc] ASC);
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetClaimKinds]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetClaimKinds] (
        [Id]                      INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AspNetClaimKinds] PRIMARY KEY CLUSTERED,
        [Name]                    NVARCHAR(128)     NOT NULL,
        [Description]             NVARCHAR(512)     NULL,
        [IncludeInAccessToken]    BIT               NOT NULL CONSTRAINT [DF_AspNetClaimKinds_IncludeInAccessToken] DEFAULT (0),
        [IncludeInTransformation] BIT               NOT NULL CONSTRAINT [DF_AspNetClaimKinds_IncludeInTransformation] DEFAULT (1),
        [IsActive]                BIT               NOT NULL CONSTRAINT [DF_AspNetClaimKinds_IsActive] DEFAULT (1)
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_AspNetClaimKinds_Name]
        ON [dbo].[AspNetClaimKinds] ([Name] ASC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetClaimKinds] WHERE [Name] = N'FrontendClaim')
BEGIN
    INSERT INTO [dbo].[AspNetClaimKinds] ([Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive])
    VALUES (N'FrontendClaim', N'UI route, guard, and presentation claims included in frontend JWT access tokens.', 1, 1, 1);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetClaimKinds] WHERE [Name] = N'BackendClaim')
BEGIN
    INSERT INTO [dbo].[AspNetClaimKinds] ([Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive])
    VALUES (N'BackendClaim', N'Server-side authorization and policy claims enriched during API request transformation.', 0, 1, 1);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetClaimKinds] WHERE [Name] = N'DataClaim')
BEGIN
    INSERT INTO [dbo].[AspNetClaimKinds] ([Name], [Description], [IncludeInAccessToken], [IncludeInTransformation], [IsActive])
    VALUES (N'DataClaim', N'Row-level, scope, and resource data filter claims enriched during API request transformation.', 0, 1, 1);
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetClaims] (
        [Id]          INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AspNetClaims] PRIMARY KEY CLUSTERED,
        [ClaimKindId] INT               NOT NULL CONSTRAINT [FK_AspNetClaims_AspNetClaimKinds_ClaimKindId] FOREIGN KEY REFERENCES [dbo].[AspNetClaimKinds] ([Id]),
        [ClaimType]   NVARCHAR(256)     NOT NULL,
        [ValueType]   NVARCHAR(256)     NOT NULL CONSTRAINT [DF_AspNetClaims_ValueType] DEFAULT (N'http://www.w3.org/2001/XMLSchema#string'),
        [DisplayName] NVARCHAR(256)     NULL,
        [Description] NVARCHAR(512)     NULL,
        [IsActive]    BIT               NOT NULL CONSTRAINT [DF_AspNetClaims_IsActive] DEFAULT (1)
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_AspNetClaims_ClaimType]
        ON [dbo].[AspNetClaims] ([ClaimType] ASC);

    CREATE NONCLUSTERED INDEX [IX_AspNetClaims_ClaimKindId]
        ON [dbo].[AspNetClaims] ([ClaimKindId] ASC);
END;
GO
