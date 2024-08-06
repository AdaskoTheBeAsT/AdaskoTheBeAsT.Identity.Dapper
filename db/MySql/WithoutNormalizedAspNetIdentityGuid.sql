--CREATE DATABASE `identity` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

CREATE TABLE WithoutNormalizedAspNetIdentityGuid.aspnetroleclaims (
    Id         INT                                  NOT NULL AUTO_INCREMENT,
    RoleId     CHAR(36)                             NOT NULL,
    ClaimType  VARCHAR(256) CHARACTER SET utf8mb4   NULL,
    ClaimValue VARCHAR(256) CHARACTER SET utf8mb4   NULL,
    CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY CLUSTERED (Id ASC)
);

CREATE INDEX IX_AspNetRoleClaims_RoleId
    ON WithoutNormalizedAspNetIdentityGuid.aspnetroleclaims(RoleId ASC);
	
CREATE TABLE WithoutNormalizedAspNetIdentityGuid.aspnetroles (
    Id               CHAR(36)     UNIQUE NOT NULL,
    Name             VARCHAR(256) CHARACTER SET utf8mb4 NULL,
    ConcurrencyStamp VARCHAR(256) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT PK_AspNetRoles PRIMARY KEY CLUSTERED (Id ASC)
);

CREATE UNIQUE INDEX RoleNameIndex
    ON WithoutNormalizedAspNetIdentityGuid.aspnetroles(Name ASC);
	
CREATE TABLE WithoutNormalizedAspNetIdentityGuid.aspnetuserclaims (
    Id         INT              NOT NULL AUTO_INCREMENT,
    UserId     CHAR(36)         NOT NULL,
    ClaimType  VARCHAR(256) CHARACTER SET utf8mb4     NULL,
    ClaimValue VARCHAR(256) CHARACTER SET utf8mb4     NULL,
    CONSTRAINT PK_AspNetUserClaims PRIMARY KEY CLUSTERED (Id ASC)
);

CREATE INDEX IX_AspNetUserClaims_UserId
    ON WithoutNormalizedAspNetIdentityGuid.aspnetuserclaims(UserId ASC);
	
CREATE TABLE WithoutNormalizedAspNetIdentityGuid.aspnetuserlogins (
    LoginProvider       VARCHAR(128) CHARACTER SET utf8mb4   NOT NULL,
    ProviderKey         VARCHAR(128) CHARACTER SET utf8mb4   NOT NULL,
    ProviderDisplayName VARCHAR(256) CHARACTER SET utf8mb4   NULL,
    UserId              CHAR(36)         NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY CLUSTERED (LoginProvider ASC, ProviderKey ASC)
);

CREATE INDEX IX_AspNetUserLogins_UserId
    ON WithoutNormalizedAspNetIdentityGuid.aspnetuserlogins(UserId ASC);
	
CREATE TABLE WithoutNormalizedAspNetIdentityGuid.aspnetuserroles (
    UserId CHAR(36)  NOT NULL,
    RoleId CHAR(36)  NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY CLUSTERED (UserId ASC, RoleId ASC)
);

CREATE INDEX IX_AspNetUserRoles_RoleId
    ON WithoutNormalizedAspNetIdentityGuid.aspnetuserroles(RoleId ASC);
	
CREATE TABLE WithoutNormalizedAspNetIdentityGuid.aspnetusers (
    Id                   CHAR(36)     UNIQUE NOT NULL,
    UserName             VARCHAR(256) CHARACTER SET utf8mb4     NULL,
    Email                VARCHAR(256) CHARACTER SET utf8mb4     NULL,
    EmailConfirmed       BIT                NOT NULL,
    PasswordHash         VARCHAR(256) CHARACTER SET utf8mb4     NULL,
    SecurityStamp        VARCHAR(256) CHARACTER SET utf8mb4     NULL,
    ConcurrencyStamp     VARCHAR(256) CHARACTER SET utf8mb4     NULL,
    PhoneNumber          VARCHAR(256) CHARACTER SET utf8mb4     NULL,
    PhoneNumberConfirmed BIT                NOT NULL,
    TwoFactorEnabled     BIT                NOT NULL,
    LockoutEnd           DATETIME           NULL,
    LockoutEnabled       BIT                NOT NULL,
    AccessFailedCount    INT                NOT NULL,
    CONSTRAINT PK_AspNetUsers PRIMARY KEY CLUSTERED (Id ASC)
);

CREATE INDEX EmailIndex
    ON WithoutNormalizedAspNetIdentityGuid.aspnetusers(Email ASC);
	
CREATE UNIQUE INDEX UserNameIndex
    ON WithoutNormalizedAspNetIdentityGuid.aspnetusers(UserName ASC);
	
CREATE TABLE WithoutNormalizedAspNetIdentityGuid.aspnetusertokens (
    UserId        CHAR(36) NOT NULL,
    LoginProvider VARCHAR(128) CHARACTER SET utf8mb4   NOT NULL,
    Name          VARCHAR(128) CHARACTER SET utf8mb4   NOT NULL,
    Value         VARCHAR(256) CHARACTER SET utf8mb4   NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY CLUSTERED (UserId ASC, LoginProvider ASC, Name ASC)
);

ALTER TABLE WithoutNormalizedAspNetIdentityGuid.aspnetroleclaims
    ADD CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES WithoutNormalizedAspNetIdentityGuid.aspnetroles (Id) ON DELETE CASCADE;
	
ALTER TABLE WithoutNormalizedAspNetIdentityGuid.aspnetuserclaims
    ADD CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES WithoutNormalizedAspNetIdentityGuid.aspnetusers (Id) ON DELETE CASCADE;

ALTER TABLE WithoutNormalizedAspNetIdentityGuid.aspnetuserlogins
    ADD CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES WithoutNormalizedAspNetIdentityGuid.aspnetusers (Id) ON DELETE CASCADE;
	
ALTER TABLE WithoutNormalizedAspNetIdentityGuid.aspnetuserroles
    ADD CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES WithoutNormalizedAspNetIdentityGuid.aspnetroles (Id) ON DELETE CASCADE;
	
ALTER TABLE WithoutNormalizedAspNetIdentityGuid.aspnetuserroles
    ADD CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES WithoutNormalizedAspNetIdentityGuid.aspnetusers (Id) ON DELETE CASCADE;
	
ALTER TABLE WithoutNormalizedAspNetIdentityGuid.aspnetusertokens
    ADD CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES WithoutNormalizedAspNetIdentityGuid.aspnetusers (Id) ON DELETE CASCADE;