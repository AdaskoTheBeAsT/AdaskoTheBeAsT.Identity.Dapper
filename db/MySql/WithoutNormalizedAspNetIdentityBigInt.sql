CREATE DATABASE `identity` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

CREATE TABLE identity.aspnetroleclaims (
    Id         INT                                  NOT NULL AUTO_INCREMENT,
    RoleId     BIGINT                               NOT NULL,
    ClaimType  VARCHAR(256) CHARACTER SET utf8mb4   NULL,
    ClaimValue VARCHAR(256) CHARACTER SET utf8mb4   NULL,
    CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY CLUSTERED (Id ASC)
);

CREATE INDEX IX_AspNetRoleClaims_RoleId
    ON identity.aspnetroleclaims(RoleId ASC);
	
CREATE TABLE identity.aspnetroles (
    Id               BIGINT     NOT NULL AUTO_INCREMENT,
    Name             VARCHAR(256) CHARACTER SET utf8mb4 NULL,
    ConcurrencyStamp VARCHAR(256) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT PK_AspNetRoles PRIMARY KEY CLUSTERED (Id ASC)
);

CREATE UNIQUE INDEX RoleNameIndex
    ON identity.aspnetroles(Name ASC);
	
CREATE TABLE identity.aspnetuserclaims (
    Id         INT              NOT NULL AUTO_INCREMENT,
    UserId     BIGINT              NOT NULL,
    ClaimType  VARCHAR(256) CHARACTER SET utf8mb4     NULL,
    ClaimValue VARCHAR(256) CHARACTER SET utf8mb4     NULL,
    CONSTRAINT PK_AspNetUserClaims PRIMARY KEY CLUSTERED (Id ASC)
);

CREATE INDEX IX_AspNetUserClaims_UserId
    ON identity.aspnetuserclaims(UserId ASC);
	
CREATE TABLE identity.aspnetuserlogins (
    LoginProvider       VARCHAR(128) CHARACTER SET utf8mb4   NOT NULL,
    ProviderKey         VARCHAR(128) CHARACTER SET utf8mb4   NOT NULL,
    ProviderDisplayName VARCHAR(256) CHARACTER SET utf8mb4   NULL,
    UserId              BIGINT          NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY CLUSTERED (LoginProvider ASC, ProviderKey ASC)
);

CREATE INDEX IX_AspNetUserLogins_UserId
    ON identity.aspnetuserlogins(UserId ASC);
	
CREATE TABLE identity.aspnetuserroles (
    UserId BIGINT  NOT NULL,
    RoleId BIGINT  NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY CLUSTERED (UserId ASC, RoleId ASC)
);

CREATE INDEX IX_AspNetUserRoles_RoleId
    ON identity.aspnetuserroles(RoleId ASC);
	
CREATE TABLE identity.aspnetusers (
    Id                   BIGINT     NOT NULL AUTO_INCREMENT,
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
    ON identity.aspnetusers(Email ASC);
	
CREATE UNIQUE INDEX UserNameIndex
    ON identity.aspnetusers(UserName ASC);
	
CREATE TABLE identity.aspnetusertokens (
    UserId        BIGINT NOT NULL,
    LoginProvider VARCHAR(128) CHARACTER SET utf8mb4   NOT NULL,
    Name          VARCHAR(128) CHARACTER SET utf8mb4   NOT NULL,
    Value         VARCHAR(256) CHARACTER SET utf8mb4   NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY CLUSTERED (UserId ASC, LoginProvider ASC, Name ASC)
);

ALTER TABLE identity.aspnetroleclaims
    ADD CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES identity.aspnetroles (Id) ON DELETE CASCADE;
	
ALTER TABLE identity.aspnetuserclaims
    ADD CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES identity.aspnetusers (Id) ON DELETE CASCADE;

ALTER TABLE identity.aspnetuserlogins
    ADD CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES identity.aspnetusers (Id) ON DELETE CASCADE;
	
ALTER TABLE identity.aspnetuserroles
    ADD CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES identity.aspnetroles (Id) ON DELETE CASCADE;
	
ALTER TABLE identity.aspnetuserroles
    ADD CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES identity.aspnetusers (Id) ON DELETE CASCADE;
	
ALTER TABLE identity.aspnetusertokens
    ADD CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES identity.aspnetusers (Id) ON DELETE CASCADE;