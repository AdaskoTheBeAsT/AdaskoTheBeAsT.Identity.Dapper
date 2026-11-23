CREATE TABLE [dbo].[IdentityJwtRefreshTokens] (
    [Id]                    UNIQUEIDENTIFIER   NOT NULL,
    [TokenHash]             CHAR (64)          NOT NULL,
    [HashKeyId]             NVARCHAR (64)      NOT NULL,
    [SessionId]             NVARCHAR (64)      NOT NULL,
    [UserId]                NVARCHAR (450)     NOT NULL,
    [SecurityStamp]         NVARCHAR (256)     NULL,
    [AuthenticationMethods] NVARCHAR (128)     NOT NULL,
    [IssuedAtUtc]           DATETIMEOFFSET (7) NOT NULL,
    [ExpiresAtUtc]          DATETIMEOFFSET (7) NOT NULL,
    [SessionExpiresAtUtc]   DATETIMEOFFSET (7) NOT NULL,
    [ConsumedAtUtc]         DATETIMEOFFSET (7) NULL,
    [ReplacedByTokenHash]   CHAR (64)          NULL,
    [RevokedAtUtc]          DATETIMEOFFSET (7) NULL,
    [RevocationReason]      NVARCHAR (128)     NULL,
    CONSTRAINT [PK_IdentityJwtRefreshTokens] PRIMARY KEY NONCLUSTERED ([Id] ASC)
);

GO
CREATE UNIQUE CLUSTERED INDEX [IX_IdentityJwtRefreshTokens_TokenHash]
    ON [dbo].[IdentityJwtRefreshTokens]([TokenHash] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_IdentityJwtRefreshTokens_SessionId]
    ON [dbo].[IdentityJwtRefreshTokens]([SessionId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_IdentityJwtRefreshTokens_UserId_Active]
    ON [dbo].[IdentityJwtRefreshTokens]([UserId] ASC)
    INCLUDE([SessionId], [ExpiresAtUtc]) WHERE ([RevokedAtUtc] IS NULL AND [ConsumedAtUtc] IS NULL);

GO
CREATE NONCLUSTERED INDEX [IX_IdentityJwtRefreshTokens_ExpiresAtUtc]
    ON [dbo].[IdentityJwtRefreshTokens]([ExpiresAtUtc] ASC);
