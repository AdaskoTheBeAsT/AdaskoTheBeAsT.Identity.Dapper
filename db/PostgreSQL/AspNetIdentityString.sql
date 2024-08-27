CREATE TABLE public.aspnetroleclaims (
    Id         INTEGER PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    RoleId     VARCHAR(36)    NOT NULL,
    ClaimType  VARCHAR(256)   NULL,
    ClaimValue VARCHAR(256)   NULL
);

CREATE INDEX IX_AspNetRoleClaims_RoleId
    ON public.aspnetroleclaims(RoleId ASC);

CREATE TABLE public.aspnetroles (
    Id               VARCHAR(36)  NOT NULL,
    Name             VARCHAR(256) NULL,
    NormalizedName   VARCHAR(256) NULL,
    ConcurrencyStamp VARCHAR(256) NULL,
    PRIMARY KEY (Id)
);

CREATE UNIQUE INDEX RoleNameIndex
    ON public.aspnetroles(NormalizedName ASC);

CREATE TABLE public.aspnetuserclaims (
    Id         INTEGER PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    UserId     VARCHAR(36)      NOT NULL,
    ClaimType  VARCHAR(256)     NULL,
    ClaimValue VARCHAR(256)     NULL
);

CREATE INDEX IX_AspNetUserClaims_UserId
    ON public.aspnetuserclaims(UserId ASC);

CREATE TABLE public.aspnetuserlogins (
    LoginProvider       VARCHAR(128)   NOT NULL,
    ProviderKey         VARCHAR(128)   NOT NULL,
    ProviderDisplayName VARCHAR(256)   NULL,
    UserId              VARCHAR(36)    NOT NULL
);

CREATE INDEX IX_AspNetUserLogins on public.aspnetuserlogins(LoginProvider ASC, ProviderKey ASC);

CREATE INDEX IX_AspNetUserLogins_UserId
    ON public.aspnetuserlogins(UserId ASC);

CREATE TABLE public.aspnetuserroles (
    UserId VARCHAR(36)  NOT NULL,
    RoleId VARCHAR(36)  NOT NULL
);

CREATE UNIQUE INDEX IX_AspNetUserRoles on public.aspnetuserroles(UserId ASC, RoleId ASC);

CREATE INDEX IX_AspNetUserRoles_RoleId
    ON public.aspnetuserroles(RoleId ASC);

CREATE TABLE public.aspnetusers (
    Id                   VARCHAR(36)      NOT NULL,
    UserName             VARCHAR(256)     NULL,
    NormalizedUserName   VARCHAR(256)     NULL,
    Email                VARCHAR(256)     NULL,
    NormalizedEmail      VARCHAR(256)     NULL,
    EmailConfirmed       BOOLEAN          NOT NULL,
    PasswordHash         VARCHAR(256)     NULL,
    SecurityStamp        VARCHAR(256)     NULL,
    ConcurrencyStamp     VARCHAR(256)     NULL,
    PhoneNumber          VARCHAR(256)     NULL,
    PhoneNumberConfirmed BOOLEAN          NOT NULL,
    TwoFactorEnabled     BOOLEAN          NOT NULL,
    LockoutEnd           TIMESTAMP        NULL,
    LockoutEnabled       BOOLEAN          NOT NULL,
    AccessFailedCount    INT              NOT NULL,
    PRIMARY KEY (Id)
);

CREATE INDEX EmailIndex
    ON public.aspnetusers(NormalizedEmail ASC);

CREATE UNIQUE INDEX UserNameIndex
    ON public.aspnetusers(NormalizedUserName ASC);

CREATE TABLE public.aspnetusertokens (
    UserId        VARCHAR(36)    NOT NULL,
    LoginProvider VARCHAR(128)   NOT NULL,
    Name          VARCHAR(128)   NOT NULL,
    Value         VARCHAR(256)   NULL
);

CREATE UNIQUE INDEX IX_AspNetUserTokens on public.aspnetusertokens(UserId ASC, LoginProvider ASC, Name ASC);

ALTER TABLE public.aspnetroleclaims
    ADD CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES public.aspnetroles (Id) ON DELETE CASCADE;

ALTER TABLE public.aspnetuserclaims
    ADD CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES public.aspnetusers (Id) ON DELETE CASCADE;

ALTER TABLE public.aspnetuserlogins
    ADD CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES public.aspnetusers (Id) ON DELETE CASCADE;

ALTER TABLE public.aspnetuserroles
    ADD CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES public.aspnetroles (Id) ON DELETE CASCADE;

ALTER TABLE public.aspnetuserroles
    ADD CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES public.aspnetusers (Id) ON DELETE CASCADE;

ALTER TABLE public.aspnetusertokens
    ADD CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES public.aspnetusers (Id) ON DELETE CASCADE;