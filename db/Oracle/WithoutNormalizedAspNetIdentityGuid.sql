CREATE TABLE ASPNETROLECLAIMS (
    Id         INTEGER GENERATED ALWAYS AS IDENTITY,
    RoleId     RAW(16) NOT NULL,
    ClaimType  VARCHAR2(256)   NULL,
    ClaimValue VARCHAR2(256)   NULL,
    PRIMARY KEY (Id)
)

/

CREATE INDEX IX_AspNetRoleClaims_RoleId
    ON ASPNETROLECLAIMS(RoleId ASC)

/

CREATE TABLE ASPNETROLES (
    Id               RAW(16) NOT NULL,
    Name             VARCHAR2(256) NULL,
    ConcurrencyStamp VARCHAR2(256) NULL,
    PRIMARY KEY (Id)
)

/

CREATE UNIQUE INDEX RoleNameIndex
    ON ASPNETROLES(Name ASC)

/
	
CREATE TABLE ASPNETUSERCLAIMS (
    Id         INTEGER GENERATED ALWAYS AS IDENTITY,
    UserId     RAW(16)         NOT NULL,
    ClaimType  VARCHAR2(256)     NULL,
    ClaimValue VARCHAR2(256)     NULL,
    PRIMARY KEY (Id)
)

/

CREATE INDEX IX_AspNetUserClaims_UserId
    ON ASPNETUSERCLAIMS(UserId ASC)

/
	
CREATE TABLE ASPNETUSERLOGINS (
    LoginProvider       VARCHAR2(128)   NOT NULL,
    ProviderKey         VARCHAR2(128)   NOT NULL,
    ProviderDisplayName VARCHAR2(256)   NULL,
    UserId              RAW(16)         NOT NULL
)

/

CREATE INDEX IX_AspNetUserLogins on ASPNETUSERLOGINS(LoginProvider ASC, ProviderKey ASC)

/

CREATE INDEX IX_AspNetUserLogins_UserId
    ON ASPNETUSERLOGINS(UserId ASC)

/
	
CREATE TABLE ASPNETUSERROLES (
    UserId RAW(16)  NOT NULL,
    RoleId RAW(16)  NOT NULL
)

/

CREATE UNIQUE INDEX IX_AspNetUserRoles on ASPNETUSERROLES(UserId ASC, RoleId ASC)

/

CREATE INDEX IX_AspNetUserRoles_RoleId
    ON ASPNETUSERROLES(RoleId ASC)

/
	
CREATE TABLE ASPNETUSERS (
    Id                   RAW(16) NOT NULL,
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
)

/

CREATE INDEX EmailIndex
    ON ASPNETUSERS(Email ASC)

/
	
CREATE UNIQUE INDEX UserNameIndex
    ON ASPNETUSERS(UserName ASC)

/
	
CREATE TABLE ASPNETUSERTOKENS (
    UserId        RAW(16) NOT NULL,
    LoginProvider VARCHAR2(128)   NOT NULL,
    Name          VARCHAR2(128)   NOT NULL,
    Value         VARCHAR2(256)   NULL
)

/

CREATE UNIQUE INDEX IX_AspNetUserTokens on ASPNETUSERTOKENS(UserId ASC, LoginProvider ASC, Name ASC)

/

ALTER TABLE ASPNETROLECLAIMS
    ADD CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES ASPNETROLES (Id) ON DELETE CASCADE

/

ALTER TABLE ASPNETUSERCLAIMS
    ADD CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES ASPNETUSERS (Id) ON DELETE CASCADE

/

ALTER TABLE ASPNETUSERLOGINS
    ADD CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES ASPNETUSERS (Id) ON DELETE CASCADE

/

ALTER TABLE ASPNETUSERROLES
    ADD CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES ASPNETROLES (Id) ON DELETE CASCADE

/
	
ALTER TABLE ASPNETUSERROLES
    ADD CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES ASPNETUSERS (Id) ON DELETE CASCADE

/

ALTER TABLE ASPNETUSERTOKENS
    ADD CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES ASPNETUSERS (Id) ON DELETE CASCADE
/