CREATE TABLE [dbo].[IdentityJwtRevocations] (
    [Id]               BIGINT             IDENTITY (1, 1) NOT NULL,
    [Issuer]           NVARCHAR (256)     NOT NULL,
    [ScopeType]        TINYINT            NOT NULL,
    [SubjectKey]       NVARCHAR (450)     NOT NULL,
    [RevokedBeforeUtc] DATETIMEOFFSET (7) NULL,
    [RevokedAtUtc]     DATETIMEOFFSET (7) NOT NULL,
    [ExpiresAtUtc]     DATETIMEOFFSET (7) NOT NULL,
    [Reason]           NVARCHAR (128)     NULL,
    CONSTRAINT [PK_IdentityJwtRevocations] PRIMARY KEY CLUSTERED ([Id] ASC)
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_IdentityJwtRevocations_Lookup]
    ON [dbo].[IdentityJwtRevocations]([Issuer] ASC, [ScopeType] ASC, [SubjectKey] ASC)
    INCLUDE([ExpiresAtUtc], [RevokedBeforeUtc]);

GO
CREATE NONCLUSTERED INDEX [IX_IdentityJwtRevocations_ExpiresAtUtc]
    ON [dbo].[IdentityJwtRevocations]([ExpiresAtUtc] ASC);
