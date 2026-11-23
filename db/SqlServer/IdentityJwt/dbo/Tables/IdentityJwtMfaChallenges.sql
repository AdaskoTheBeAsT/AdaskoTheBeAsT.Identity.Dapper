CREATE TABLE [dbo].[IdentityJwtMfaChallenges] (
    [ChallengeId]          UNIQUEIDENTIFIER   NOT NULL,
    [TokenHash]            CHAR (64)          NOT NULL,
    [HashKeyId]            NVARCHAR (64)      NOT NULL,
    [Purpose]              TINYINT            NOT NULL,
    [UserId]               NVARCHAR (450)     NOT NULL,
    [SecurityStamp]        NVARCHAR (256)     NULL,
    [AllowedMethods]       NVARCHAR (128)     NOT NULL,
    [SelectedMethod]       NVARCHAR (32)      NULL,
    [OtpCodeHash]          CHAR (64)          NULL,
    [AttemptCount]         INT                NOT NULL,
    [MaxAttempts]          INT                NOT NULL,
    [ResendCount]          INT                NOT NULL,
    [ResendAvailableAtUtc] DATETIMEOFFSET (7) NULL,
    [CreatedAtUtc]         DATETIMEOFFSET (7) NOT NULL,
    [ExpiresAtUtc]         DATETIMEOFFSET (7) NOT NULL,
    CONSTRAINT [PK_IdentityJwtMfaChallenges] PRIMARY KEY NONCLUSTERED ([ChallengeId] ASC)
);

GO
CREATE UNIQUE CLUSTERED INDEX [IX_IdentityJwtMfaChallenges_TokenHash]
    ON [dbo].[IdentityJwtMfaChallenges]([TokenHash] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_IdentityJwtMfaChallenges_ExpiresAtUtc]
    ON [dbo].[IdentityJwtMfaChallenges]([ExpiresAtUtc] ASC);
