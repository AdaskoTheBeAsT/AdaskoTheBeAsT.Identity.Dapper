CREATE TABLE [dbo].[AspNetUsers] (
    [Id]                   UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWSEQUENTIALID(),
    [UserName]             NVARCHAR (256)     NULL,
    [Email]                NVARCHAR (256)     NULL,
    [EmailConfirmed]       BIT                NOT NULL,
    [PasswordHash]         NVARCHAR (128)     NULL,
    [SecurityStamp]        NVARCHAR (128)     NULL,
    [ConcurrencyStamp]     NVARCHAR (36)      NULL,
    [PhoneNumber]          NVARCHAR (128)     NULL,
    [PhoneNumberConfirmed] BIT                NOT NULL,
    [TwoFactorEnabled]     BIT                NOT NULL,
    [LockoutEnd]           DATETIMEOFFSET (7) NULL,
    [LockoutEnabled]       BIT                NOT NULL,
    [AccessFailedCount]    INT                NOT NULL,
    [SysStart]             DATETIME2 (7) GENERATED ALWAYS AS ROW START NOT NULL,
	[SysEnd]               DATETIME2 (7) GENERATED ALWAYS AS ROW END NOT NULL,
	PERIOD FOR SYSTEM_TIME ([SysStart], [SysEnd]),
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
)
WITH (SYSTEM_VERSIONING = ON(HISTORY_TABLE=[dbo].[AspNetUsers_HISTORY], DATA_CONSISTENCY_CHECK=ON));


GO
CREATE NONCLUSTERED INDEX [EmailIndex]
    ON [dbo].[AspNetUsers]([Email] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex]
    ON [dbo].[AspNetUsers]([UserName] ASC) WHERE ([UserName] IS NOT NULL);

