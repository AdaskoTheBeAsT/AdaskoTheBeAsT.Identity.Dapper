CREATE TABLE [dbo].[AspNetRoles] (
    [Id]               INT IDENTITY (1, 1) NOT NULL,
    [Name]             NVARCHAR (256) NULL,
    [ConcurrencyStamp] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex]
    ON [dbo].[AspNetRoles]([Name] ASC) WHERE ([Name] IS NOT NULL);

