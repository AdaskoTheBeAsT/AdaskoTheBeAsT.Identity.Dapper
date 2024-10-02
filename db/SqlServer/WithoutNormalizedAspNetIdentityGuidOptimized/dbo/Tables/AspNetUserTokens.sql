CREATE TABLE [dbo].[AspNetUserTokens] (
    [UserId]        UNIQUEIDENTIFIER NOT NULL,
    [LoginProvider] NVARCHAR (128) NOT NULL,
    [Name]          NVARCHAR (128) NOT NULL,
    [Value]         NVARCHAR (4096) NULL,
    [SysStart]      DATETIME2 (7) GENERATED ALWAYS AS ROW START NOT NULL,
	[SysEnd]        DATETIME2 (7) GENERATED ALWAYS AS ROW END NOT NULL,
	PERIOD FOR SYSTEM_TIME ([SysStart], [SysEnd]),
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED ([UserId] ASC, [LoginProvider] ASC, [Name] ASC),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
)
WITH (SYSTEM_VERSIONING = ON(HISTORY_TABLE=[dbo].[AspNetUserTokens_HISTORY], DATA_CONSISTENCY_CHECK=ON));

