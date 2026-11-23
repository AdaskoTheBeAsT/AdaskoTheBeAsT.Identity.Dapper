-- Identity JWT runtime tables and default claim kinds (default provider options).

BEGIN EXECUTE IMMEDIATE 'CREATE TABLE "identityjwt_locks" (bucket VARCHAR2(16) NOT NULL PRIMARY KEY, stamp INTEGER NOT NULL)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE TABLE "identityjwt_refresh" (
    row_key VARCHAR2(64) NOT NULL PRIMARY KEY,
    payload CLOB NOT NULL,
    expiry NUMBER(19) NOT NULL,
    scope_key VARCHAR2(64),
    owner_key VARCHAR2(64),
    ordinal INTEGER NOT NULL,
    revoked NUMBER(19),
    reason VARCHAR2(128)
)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE TABLE "identityjwt_mfa" (
    row_key VARCHAR2(64) NOT NULL PRIMARY KEY,
    payload CLOB NOT NULL,
    expiry NUMBER(19) NOT NULL,
    scope_key VARCHAR2(64),
    owner_key VARCHAR2(64),
    ordinal INTEGER NOT NULL,
    revoked NUMBER(19),
    reason VARCHAR2(128)
)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE TABLE "identityjwt_revocations" (
    row_key VARCHAR2(64) NOT NULL PRIMARY KEY,
    payload CLOB NOT NULL,
    expiry NUMBER(19) NOT NULL,
    scope_key VARCHAR2(64),
    owner_key VARCHAR2(64),
    ordinal INTEGER NOT NULL,
    revoked NUMBER(19),
    reason VARCHAR2(128)
)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE TABLE "identityjwt_kinds" (
    row_key VARCHAR2(64) NOT NULL PRIMARY KEY,
    payload CLOB NOT NULL,
    expiry NUMBER(19) NOT NULL,
    scope_key VARCHAR2(64),
    owner_key VARCHAR2(64),
    ordinal INTEGER NOT NULL,
    revoked NUMBER(19),
    reason VARCHAR2(128)
)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE TABLE "identityjwt_claims" (
    row_key VARCHAR2(64) NOT NULL PRIMARY KEY,
    payload CLOB NOT NULL,
    expiry NUMBER(19) NOT NULL,
    scope_key VARCHAR2(64),
    owner_key VARCHAR2(64),
    ordinal INTEGER NOT NULL,
    revoked NUMBER(19),
    reason VARCHAR2(128)
)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE INDEX "identityjwt_r_scope" ON "identityjwt_refresh" (scope_key)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE INDEX "identityjwt_r_owner" ON "identityjwt_refresh" (owner_key)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE INDEX "identityjwt_r_exp" ON "identityjwt_refresh" (expiry, row_key)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE INDEX "identityjwt_m_exp" ON "identityjwt_mfa" (expiry, row_key)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE INDEX "identityjwt_v_exp" ON "identityjwt_revocations" (expiry, row_key)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE INDEX "identityjwt_k_ord" ON "identityjwt_kinds" (ordinal)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE INDEX "identityjwt_c_ord" ON "identityjwt_claims" (ordinal)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

BEGIN EXECUTE IMMEDIATE 'CREATE INDEX "identityjwt_c_kind" ON "identityjwt_claims" (scope_key, ordinal)'; EXCEPTION WHEN OTHERS THEN IF SQLCODE <> -955 THEN RAISE; END IF; END;
/

MERGE INTO "identityjwt_locks" t USING (SELECT 'refresh' bucket FROM dual) s ON (t.bucket = s.bucket) WHEN NOT MATCHED THEN INSERT (bucket, stamp) VALUES (s.bucket, 0);

MERGE INTO "identityjwt_locks" t USING (SELECT 'mfa' bucket FROM dual) s ON (t.bucket = s.bucket) WHEN NOT MATCHED THEN INSERT (bucket, stamp) VALUES (s.bucket, 0);

MERGE INTO "identityjwt_locks" t USING (SELECT 'revocations' bucket FROM dual) s ON (t.bucket = s.bucket) WHEN NOT MATCHED THEN INSERT (bucket, stamp) VALUES (s.bucket, 0);

MERGE INTO "identityjwt_locks" t USING (SELECT 'catalog' bucket FROM dual) s ON (t.bucket = s.bucket) WHEN NOT MATCHED THEN INSERT (bucket, stamp) VALUES (s.bucket, 0);

MERGE INTO "identityjwt_kinds" t USING (SELECT '7F4FB6828680C689B38D4DC96A33B6606F0AA4C462B6C1BC439FD74175570C99' row_key FROM dual) s ON (t.row_key = s.row_key) WHEN NOT MATCHED THEN INSERT (row_key, payload, expiry, ordinal) VALUES ('7F4FB6828680C689B38D4DC96A33B6606F0AA4C462B6C1BC439FD74175570C99', '{"Id":1,"Name":"FrontendClaim","Description":null,"IncludeInAccessToken":true,"IncludeInTransformation":true,"IsActive":true}', 9223372036854775807, 1);

MERGE INTO "identityjwt_kinds" t USING (SELECT '0673521892BC0B82ED1ACDE10112DC64AE034D02151FF474C940B72CD862A507' row_key FROM dual) s ON (t.row_key = s.row_key) WHEN NOT MATCHED THEN INSERT (row_key, payload, expiry, ordinal) VALUES ('0673521892BC0B82ED1ACDE10112DC64AE034D02151FF474C940B72CD862A507', '{"Id":2,"Name":"BackendClaim","Description":null,"IncludeInAccessToken":false,"IncludeInTransformation":true,"IsActive":true}', 9223372036854775807, 2);

MERGE INTO "identityjwt_kinds" t USING (SELECT '036AAB780528FA21D28E69BCBF598B0D49AD1B273FA10F0E683A111DB50BD61D' row_key FROM dual) s ON (t.row_key = s.row_key) WHEN NOT MATCHED THEN INSERT (row_key, payload, expiry, ordinal) VALUES ('036AAB780528FA21D28E69BCBF598B0D49AD1B273FA10F0E683A111DB50BD61D', '{"Id":3,"Name":"DataClaim","Description":null,"IncludeInAccessToken":false,"IncludeInTransformation":true,"IsActive":true}', 9223372036854775807, 3);

COMMIT;
