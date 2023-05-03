-- connect to service xepdb1 as SYS as SYDBA
CREATE USER mydeveloper IDENTIFIED BY mypassword QUOTA UNLIMITED ON users;
GRANT connect,resource,create view to mydeveloper;

-- relog as mydeveloper

-- create helper functions
CREATE OR REPLACE FUNCTION GUID_FROM_RAW(raw_guid IN RAW) RETURN VARCHAR2 IS
    hex_guid VARCHAR2(32);
BEGIN
    hex_guid := RAWTOHEX(raw_guid);
    RETURN SUBSTR(hex_guid, 7, 2) || SUBSTR(hex_guid, 5, 2) ||
            SUBSTR(hex_guid, 3, 2) || SUBSTR(hex_guid, 1, 2) ||
            '-' || SUBSTR(hex_guid, 11, 2) || SUBSTR(hex_guid, 9, 2) ||
            '-' || SUBSTR(hex_guid, 15, 2) || SUBSTR(hex_guid, 13, 2) ||
            '-' || SUBSTR(hex_guid, 17, 4) ||
            '-' || SUBSTR(hex_guid, 21, 12);
END;


CREATE OR REPLACE FUNCTION RAW_FROM_GUID(guid_str IN VARCHAR2) RETURN RAW IS
  hex_guid VARCHAR2(32);
BEGIN
  hex_guid := REPLACE(guid_str, '-', ''); -- Remove hyphens
  RETURN HEXTORAW(SUBSTR(hex_guid, 7, 2) || SUBSTR(hex_guid, 5, 2) ||
                  SUBSTR(hex_guid, 3, 2) || SUBSTR(hex_guid, 1, 2) ||
                  SUBSTR(hex_guid, 11, 2) || SUBSTR(hex_guid, 9, 2) ||
                  SUBSTR(hex_guid, 15, 2) || SUBSTR(hex_guid, 13, 2) ||
                  SUBSTR(hex_guid, 17, 4) ||
                  SUBSTR(hex_guid, 21, 12));
END;