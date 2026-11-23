-- Identity JWT runtime tables and default claim kinds (default provider options).

CREATE TABLE IF NOT EXISTS `identityjwt_locks` (bucket VARCHAR(16) NOT NULL PRIMARY KEY, stamp INTEGER NOT NULL) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `identityjwt_refresh` (
    row_key VARCHAR(64) NOT NULL PRIMARY KEY,
    payload LONGTEXT NOT NULL,
    expiry BIGINT NOT NULL,
    scope_key VARCHAR(64),
    owner_key VARCHAR(64),
    ordinal INTEGER NOT NULL,
    revoked BIGINT,
    reason VARCHAR(128), INDEX `identityjwt_r_scope` (scope_key), INDEX `identityjwt_r_owner` (owner_key), INDEX `identityjwt_r_exp` (expiry, row_key)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `identityjwt_mfa` (
    row_key VARCHAR(64) NOT NULL PRIMARY KEY,
    payload LONGTEXT NOT NULL,
    expiry BIGINT NOT NULL,
    scope_key VARCHAR(64),
    owner_key VARCHAR(64),
    ordinal INTEGER NOT NULL,
    revoked BIGINT,
    reason VARCHAR(128), INDEX `identityjwt_m_exp` (expiry, row_key)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `identityjwt_revocations` (
    row_key VARCHAR(64) NOT NULL PRIMARY KEY,
    payload LONGTEXT NOT NULL,
    expiry BIGINT NOT NULL,
    scope_key VARCHAR(64),
    owner_key VARCHAR(64),
    ordinal INTEGER NOT NULL,
    revoked BIGINT,
    reason VARCHAR(128), INDEX `identityjwt_v_exp` (expiry, row_key)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `identityjwt_kinds` (
    row_key VARCHAR(64) NOT NULL PRIMARY KEY,
    payload LONGTEXT NOT NULL,
    expiry BIGINT NOT NULL,
    scope_key VARCHAR(64),
    owner_key VARCHAR(64),
    ordinal INTEGER NOT NULL,
    revoked BIGINT,
    reason VARCHAR(128), INDEX `identityjwt_k_ord` (ordinal)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `identityjwt_claims` (
    row_key VARCHAR(64) NOT NULL PRIMARY KEY,
    payload LONGTEXT NOT NULL,
    expiry BIGINT NOT NULL,
    scope_key VARCHAR(64),
    owner_key VARCHAR(64),
    ordinal INTEGER NOT NULL,
    revoked BIGINT,
    reason VARCHAR(128), INDEX `identityjwt_c_ord` (ordinal), INDEX `identityjwt_c_kind` (scope_key, ordinal)
) ENGINE=InnoDB;

INSERT INTO `identityjwt_locks` (bucket, stamp) VALUES ('refresh', 0) ON DUPLICATE KEY UPDATE stamp = stamp;

INSERT INTO `identityjwt_locks` (bucket, stamp) VALUES ('mfa', 0) ON DUPLICATE KEY UPDATE stamp = stamp;

INSERT INTO `identityjwt_locks` (bucket, stamp) VALUES ('revocations', 0) ON DUPLICATE KEY UPDATE stamp = stamp;

INSERT INTO `identityjwt_locks` (bucket, stamp) VALUES ('catalog', 0) ON DUPLICATE KEY UPDATE stamp = stamp;

INSERT INTO `identityjwt_kinds` (row_key, payload, expiry, ordinal) VALUES ('7F4FB6828680C689B38D4DC96A33B6606F0AA4C462B6C1BC439FD74175570C99', '{"Id":1,"Name":"FrontendClaim","Description":null,"IncludeInAccessToken":true,"IncludeInTransformation":true,"IsActive":true}', 9223372036854775807, 1) ON DUPLICATE KEY UPDATE row_key = row_key;

INSERT INTO `identityjwt_kinds` (row_key, payload, expiry, ordinal) VALUES ('0673521892BC0B82ED1ACDE10112DC64AE034D02151FF474C940B72CD862A507', '{"Id":2,"Name":"BackendClaim","Description":null,"IncludeInAccessToken":false,"IncludeInTransformation":true,"IsActive":true}', 9223372036854775807, 2) ON DUPLICATE KEY UPDATE row_key = row_key;

INSERT INTO `identityjwt_kinds` (row_key, payload, expiry, ordinal) VALUES ('036AAB780528FA21D28E69BCBF598B0D49AD1B273FA10F0E683A111DB50BD61D', '{"Id":3,"Name":"DataClaim","Description":null,"IncludeInAccessToken":false,"IncludeInTransformation":true,"IsActive":true}', 9223372036854775807, 3) ON DUPLICATE KEY UPDATE row_key = row_key;
