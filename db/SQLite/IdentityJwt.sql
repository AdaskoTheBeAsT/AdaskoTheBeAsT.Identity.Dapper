-- Identity JWT runtime tables and default claim kinds (default provider options).

CREATE TABLE IF NOT EXISTS "identityjwt_locks" (bucket VARCHAR(16) NOT NULL PRIMARY KEY, stamp INTEGER NOT NULL);

CREATE TABLE IF NOT EXISTS "identityjwt_refresh" (
    row_key VARCHAR(64) NOT NULL PRIMARY KEY,
    payload TEXT NOT NULL,
    expiry BIGINT NOT NULL,
    scope_key VARCHAR(64),
    owner_key VARCHAR(64),
    ordinal INTEGER NOT NULL,
    revoked BIGINT,
    reason VARCHAR(128)
);

CREATE TABLE IF NOT EXISTS "identityjwt_mfa" (
    row_key VARCHAR(64) NOT NULL PRIMARY KEY,
    payload TEXT NOT NULL,
    expiry BIGINT NOT NULL,
    scope_key VARCHAR(64),
    owner_key VARCHAR(64),
    ordinal INTEGER NOT NULL,
    revoked BIGINT,
    reason VARCHAR(128)
);

CREATE TABLE IF NOT EXISTS "identityjwt_revocations" (
    row_key VARCHAR(64) NOT NULL PRIMARY KEY,
    payload TEXT NOT NULL,
    expiry BIGINT NOT NULL,
    scope_key VARCHAR(64),
    owner_key VARCHAR(64),
    ordinal INTEGER NOT NULL,
    revoked BIGINT,
    reason VARCHAR(128)
);

CREATE TABLE IF NOT EXISTS "identityjwt_kinds" (
    row_key VARCHAR(64) NOT NULL PRIMARY KEY,
    payload TEXT NOT NULL,
    expiry BIGINT NOT NULL,
    scope_key VARCHAR(64),
    owner_key VARCHAR(64),
    ordinal INTEGER NOT NULL,
    revoked BIGINT,
    reason VARCHAR(128)
);

CREATE TABLE IF NOT EXISTS "identityjwt_claims" (
    row_key VARCHAR(64) NOT NULL PRIMARY KEY,
    payload TEXT NOT NULL,
    expiry BIGINT NOT NULL,
    scope_key VARCHAR(64),
    owner_key VARCHAR(64),
    ordinal INTEGER NOT NULL,
    revoked BIGINT,
    reason VARCHAR(128)
);

CREATE INDEX IF NOT EXISTS "identityjwt_r_scope" ON "identityjwt_refresh" (scope_key);

CREATE INDEX IF NOT EXISTS "identityjwt_r_owner" ON "identityjwt_refresh" (owner_key);

CREATE INDEX IF NOT EXISTS "identityjwt_r_exp" ON "identityjwt_refresh" (expiry, row_key);

CREATE INDEX IF NOT EXISTS "identityjwt_m_exp" ON "identityjwt_mfa" (expiry, row_key);

CREATE INDEX IF NOT EXISTS "identityjwt_v_exp" ON "identityjwt_revocations" (expiry, row_key);

CREATE INDEX IF NOT EXISTS "identityjwt_k_ord" ON "identityjwt_kinds" (ordinal);

CREATE INDEX IF NOT EXISTS "identityjwt_c_ord" ON "identityjwt_claims" (ordinal);

CREATE INDEX IF NOT EXISTS "identityjwt_c_kind" ON "identityjwt_claims" (scope_key, ordinal);

INSERT INTO "identityjwt_locks" (bucket, stamp) VALUES ('refresh', 0) ON CONFLICT (bucket) DO NOTHING;

INSERT INTO "identityjwt_locks" (bucket, stamp) VALUES ('mfa', 0) ON CONFLICT (bucket) DO NOTHING;

INSERT INTO "identityjwt_locks" (bucket, stamp) VALUES ('revocations', 0) ON CONFLICT (bucket) DO NOTHING;

INSERT INTO "identityjwt_locks" (bucket, stamp) VALUES ('catalog', 0) ON CONFLICT (bucket) DO NOTHING;

INSERT INTO "identityjwt_kinds" (row_key, payload, expiry, ordinal) VALUES ('7F4FB6828680C689B38D4DC96A33B6606F0AA4C462B6C1BC439FD74175570C99', '{"Id":1,"Name":"FrontendClaim","Description":null,"IncludeInAccessToken":true,"IncludeInTransformation":true,"IsActive":true}', 9223372036854775807, 1) ON CONFLICT (row_key) DO NOTHING;

INSERT INTO "identityjwt_kinds" (row_key, payload, expiry, ordinal) VALUES ('0673521892BC0B82ED1ACDE10112DC64AE034D02151FF474C940B72CD862A507', '{"Id":2,"Name":"BackendClaim","Description":null,"IncludeInAccessToken":false,"IncludeInTransformation":true,"IsActive":true}', 9223372036854775807, 2) ON CONFLICT (row_key) DO NOTHING;

INSERT INTO "identityjwt_kinds" (row_key, payload, expiry, ordinal) VALUES ('036AAB780528FA21D28E69BCBF598B0D49AD1B273FA10F0E683A111DB50BD61D', '{"Id":3,"Name":"DataClaim","Description":null,"IncludeInAccessToken":false,"IncludeInTransformation":true,"IsActive":true}', 9223372036854775807, 3) ON CONFLICT (row_key) DO NOTHING;
