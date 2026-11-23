CREATE TABLE [dbo].[AspNetClaimKinds] (
    [Id]                      INT            IDENTITY (1, 1) NOT NULL,
    [Name]                    NVARCHAR (128) NOT NULL,
    [Description]             NVARCHAR (512) NULL,
    [IncludeInAccessToken]    BIT            CONSTRAINT [DF_AspNetClaimKinds_IncludeInAccessToken] DEFAULT ((0)) NOT NULL,
    [IncludeInTransformation] BIT            CONSTRAINT [DF_AspNetClaimKinds_IncludeInTransformation] DEFAULT ((1)) NOT NULL,
    [IsActive]                BIT            CONSTRAINT [DF_AspNetClaimKinds_IsActive] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_AspNetClaimKinds] PRIMARY KEY CLUSTERED ([Id] ASC)
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_AspNetClaimKinds_Name]
    ON [dbo].[AspNetClaimKinds]([Name] ASC);
