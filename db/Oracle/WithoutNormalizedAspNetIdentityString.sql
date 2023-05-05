CREATE TABLE mydeveloper.ASPNETROLECLAIMS (
    Id         INTEGER GENERATED ALWAYS AS IDENTITY,
    RoleId     CHAR(36) NOT NULL,
    ClaimType  VARCHAR2(256)   NULL,
    ClaimValue VARCHAR2(256)   NULL,
    PRIMARY KEY (Id)
);

CREATE INDEX IX_AspNetRoleClaims_RoleId
    ON mydeveloper.ASPNETROLECLAIMS(RoleId ASC);
	
CREATE TABLE mydeveloper.ASPNETROLES (
    Id               CHAR(36) NOT NULL,
    Name             VARCHAR2(256) NULL,
    ConcurrencyStamp VARCHAR2(256) NULL,
    PRIMARY KEY (Id)
);

CREATE UNIQUE INDEX RoleNameIndex
    ON mydeveloper.ASPNETROLES(Name ASC);
	
CREATE TABLE mydeveloper.ASPNETUSERCLAIMS (
    Id         INTEGER GENERATED ALWAYS AS IDENTITY,
    UserId     CHAR(36)         NOT NULL,
    ClaimType  VARCHAR2(256)     NULL,
    ClaimValue VARCHAR2(256)     NULL,
    PRIMARY KEY (Id)
);

CREATE INDEX IX_AspNetUserClaims_UserId
    ON mydeveloper.ASPNETUSERCLAIMS(UserId ASC);
	
CREATE TABLE mydeveloper.ASPNETUSERLOGINS (
    LoginProvider       VARCHAR2(128)   NOT NULL,
    ProviderKey         VARCHAR2(128)   NOT NULL,
    ProviderDisplayName VARCHAR2(256)   NULL,
    UserId              CHAR(36)         NOT NULL
);

CREATE INDEX IX_AspNetUserLogins on mydeveloper.ASPNETUSERLOGINS(LoginProvider ASC, ProviderKey ASC);

CREATE INDEX IX_AspNetUserLogins_UserId
    ON mydeveloper.ASPNETUSERLOGINS(UserId ASC);
	
CREATE TABLE mydeveloper.ASPNETUSERROLES (
    UserId CHAR(36)  NOT NULL,
    RoleId CHAR(36)  NOT NULL
);

CREATE UNIQUE INDEX IX_AspNetUserRoles on mydeveloper.ASPNETUSERROLES(UserId ASC, RoleId ASC);

CREATE INDEX IX_AspNetUserRoles_RoleId
    ON mydeveloper.ASPNETUSERROLES(RoleId ASC);
	
CREATE TABLE mydeveloper.ASPNETUSERS (
    Id                   CHAR(36) NOT NULL,
    UserName             VARCHAR2(256)     NULL,
    Email                VARCHAR2(256)     NULL,
    EmailConfirmed       CHAR(1)                NOT NULL,
    PasswordHash         VARCHAR2(256)     NULL,
    SecurityStamp        VARCHAR2(256)     NULL,
    ConcurrencyStamp     VARCHAR2(256)     NULL,
    PhoneNumber          VARCHAR2(256)     NULL,
    PhoneNumberConfirmed CHAR(1)                 NOT NULL,
    TwoFactorEnabled     CHAR(1)                 NOT NULL,
    LockoutEnd           TIMESTAMP           NULL,
    LockoutEnabled       CHAR(1)                 NOT NULL,
    AccessFailedCount    INT                NOT NULL,
    PRIMARY KEY (Id)
);

CREATE INDEX EmailIndex
    ON mydeveloper.ASPNETUSERS(Email ASC);
	
CREATE UNIQUE INDEX UserNameIndex
    ON mydeveloper.ASPNETUSERS(UserName ASC);
	
CREATE TABLE mydeveloper.ASPNETUSERTOKENS (
    UserId        CHAR(36) NOT NULL,
    LoginProvider VARCHAR2(128)   NOT NULL,
    Name          VARCHAR2(128)   NOT NULL,
    Value         VARCHAR2(256)   NULL
);

CREATE UNIQUE INDEX IX_AspNetUserTokens on mydeveloper.ASPNETUSERTOKENS(UserId ASC, LoginProvider ASC, Name ASC);

ALTER TABLE mydeveloper.ASPNETROLECLAIMS
    ADD CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES mydeveloper.ASPNETROLES (Id) ON DELETE CASCADE;
	
ALTER TABLE mydeveloper.ASPNETUSERCLAIMS
    ADD CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES mydeveloper.ASPNETUSERS (Id) ON DELETE CASCADE;

ALTER TABLE mydeveloper.ASPNETUSERLOGINS
    ADD CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES mydeveloper.ASPNETUSERS (Id) ON DELETE CASCADE;
	
ALTER TABLE mydeveloper.ASPNETUSERROLES
    ADD CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES mydeveloper.ASPNETROLES (Id) ON DELETE CASCADE;
	
ALTER TABLE mydeveloper.ASPNETUSERROLES
    ADD CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES mydeveloper.ASPNETUSERS (Id) ON DELETE CASCADE;
	
ALTER TABLE mydeveloper.ASPNETUSERTOKENS
    ADD CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES mydeveloper.ASPNETUSERS (Id) ON DELETE CASCADE;
