CREATE DOMAIN MD5 AS
CHAR(32) CHARACTER SET ASCII
COLLATE ASCII;

CREATE TABLE LIST (
    MD5     MD5 NOT NULL,
    DATA  BLOB SUB_TYPE 0 SEGMENT SIZE 80 NOT NULL
);
