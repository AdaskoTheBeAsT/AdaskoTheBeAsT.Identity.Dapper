CREATE TABLE [dbo].[AspNetClaims] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [ClaimKindId] INT            NOT NULL,
    [ClaimType]   NVARCHAR (256) NOT NULL,
    [ValueType]   NVARCHAR (256) CONSTRAINT [DF_AspNetClaims_ValueType] DEFAULT (N'http://www.w3.org/2001/XMLSchema#string') NOT NULL,
    [DisplayName] NVARCHAR (256) NULL,
    [Description] NVARCHAR (512) NULL,
    [IsActive]    BIT            CONSTRAINT [DF_AspNetClaims_IsActive] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_AspNetClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AspNetClaims_AspNetClaimKinds_ClaimKindId] FOREIGN KEY ([ClaimKindId]) REFERENCES [dbo].[AspNetClaimKinds] ([Id])
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_AspNetClaims_ClaimType]
    ON [dbo].[AspNetClaims]([ClaimType] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_AspNetClaims_ClaimKindId]
    ON [dbo].[AspNetClaims]([ClaimKindId] ASC);
