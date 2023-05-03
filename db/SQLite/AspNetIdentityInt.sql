CREATE TABLE aspnetroles (
    Id               INT     NOT NULL PRIMARY KEY,
    Name             VARCHAR(256) NULL,
    NormalizedName   VARCHAR(256) NULL,
    ConcurrencyStamp VARCHAR(256) NULL
);

CREATE UNIQUE INDEX RoleNameIndex
    ON aspnetroles(NormalizedName ASC);
    
CREATE TABLE aspnetusers (
    Id                   INT     NOT NULL PRIMARY KEY,
    UserName             VARCHAR(256)     NULL,
    NormalizedUserName   VARCHAR(256)     NULL,
    Email                VARCHAR(256)     NULL,
    NormalizedEmail      VARCHAR(256)     NULL,
    EmailConfirmed       BIT                NOT NULL,
    PasswordHash         VARCHAR(256)     NULL,
    SecurityStamp        VARCHAR(256)     NULL,
    ConcurrencyStamp     VARCHAR(256)     NULL,
    PhoneNumber          VARCHAR(256)     NULL,
    PhoneNumberConfirmed BIT                NOT NULL,
    TwoFactorEnabled     BIT                NOT NULL,
    LockoutEnd           DATETIME           NULL,
    LockoutEnabled       BIT                NOT NULL,
    AccessFailedCount    INT                NOT NULL
);

CREATE INDEX EmailIndex
    ON aspnetusers(NormalizedEmail ASC);
	
CREATE UNIQUE INDEX UserNameIndex
    ON aspnetusers(NormalizedUserName ASC);

CREATE TABLE aspnetroleclaims (
    Id         INTEGER NOT NULL PRIMARY KEY,
    RoleId     INT NOT NULL,
    ClaimType  VARCHAR(256)   NULL,
    ClaimValue VARCHAR(256)   NULL,
    FOREIGN KEY (RoleId) REFERENCES aspnetroles (Id)
);

CREATE INDEX IX_AspNetRoleClaims_RoleId
    ON aspnetroleclaims(RoleId ASC);
	
CREATE TABLE aspnetuserclaims (
    Id         INT              NOT NULL PRIMARY KEY,
    UserId     INT              NOT NULL,
    ClaimType  VARCHAR(256)     NULL,
    ClaimValue VARCHAR(256)     NULL,
    FOREIGN KEY (UserId) REFERENCES aspnetusers (Id)
);

CREATE INDEX IX_AspNetUserClaims_UserId
    ON aspnetuserclaims(UserId ASC);
	
CREATE TABLE aspnetuserlogins (
    LoginProvider       VARCHAR(128)   NOT NULL,
    ProviderKey         VARCHAR(128)   NOT NULL,
    ProviderDisplayName VARCHAR(256)   NULL,
    UserId              INT          NOT NULL,
    PRIMARY KEY (LoginProvider ASC, ProviderKey ASC),
    FOREIGN KEY (UserId) REFERENCES aspnetusers (Id)
);

CREATE INDEX IX_AspNetUserLogins_UserId
    ON aspnetuserlogins(UserId ASC);
	
CREATE TABLE aspnetuserroles (
    UserId INT  NOT NULL,
    RoleId INT  NOT NULL,
    PRIMARY KEY (UserId ASC, RoleId ASC),
    FOREIGN KEY (RoleId) REFERENCES aspnetroles (Id),
    FOREIGN KEY (UserId) REFERENCES aspnetusers (Id)
);

CREATE INDEX IX_AspNetUserRoles_RoleId
    ON aspnetuserroles(RoleId ASC);
	

	
CREATE TABLE aspnetusertokens (
    UserId        INT NOT NULL,
    LoginProvider VARCHAR(128)   NOT NULL,
    Name          VARCHAR(128)   NOT NULL,
    Value         VARCHAR(256)   NULL,
    PRIMARY KEY (UserId ASC, LoginProvider ASC, Name ASC),
    FOREIGN KEY (UserId) REFERENCES aspnetusers (Id)
);
