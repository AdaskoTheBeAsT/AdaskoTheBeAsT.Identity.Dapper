CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id]         INT            IDENTITY (1, 1) NOT NULL,
    [UserId]     UNIQUEIDENTIFIER NOT NULL,
    [ClaimType]  NVARCHAR (MAX) NULL,
    [ClaimValue] NVARCHAR (MAX) NULL,
    [SysStart]   DATETIME2 (7) GENERATED ALWAYS AS ROW START NOT NULL,
	[SysEnd]     DATETIME2 (7) GENERATED ALWAYS AS ROW END NOT NULL,
	PERIOD FOR SYSTEM_TIME ([SysStart], [SysEnd]),
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
)
WITH (SYSTEM_VERSIONING = ON(HISTORY_TABLE=[dbo].[AspNetUserClaims_HISTORY], DATA_CONSISTENCY_CHECK=ON));


GO
CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId]
    ON [dbo].[AspNetUserClaims]([UserId] ASC);

