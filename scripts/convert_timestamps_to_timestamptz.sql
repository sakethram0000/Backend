-- convert_timestamps_to_timestamptz.sql
-- Safe helper script to convert text/date-like columns to timestamptz in Postgres.
-- IMPORTANT: Backup your database before running. Test on staging first.
-- Usage (psql): psql "postgresql://user:pass@host:port/dbname" -f convert_timestamps_to_timestamptz.sql

BEGIN;

-- 1) Create lightweight backups of tables involved (schema + data) - you may skip or adjust for large datasets
DROP TABLE IF EXISTS users_backup;
CREATE TABLE users_backup AS TABLE users;

DROP TABLE IF EXISTS products_backup;
CREATE TABLE products_backup AS TABLE products;

DROP TABLE IF EXISTS rules_backup;
CREATE TABLE rules_backup AS TABLE rules;

DROP TABLE IF EXISTS events_backup;
CREATE TABLE events_backup AS TABLE events;

DROP TABLE IF EXISTS submissions_backup;
CREATE TABLE submissions_backup AS TABLE submissions;

DROP TABLE IF EXISTS carriers_backup;
CREATE TABLE carriers_backup AS TABLE carriers;

-- 2) Check current column types. Run these SELECTs first to inspect in your environment.
-- SELECT column_name, data_type, udt_name FROM information_schema.columns WHERE table_name='users' AND column_name IN ('createdat','lastloginat','lockoutend','passwordresetexpiry');
-- SELECT column_name, data_type, udt_name FROM information_schema.columns WHERE table_name='products' AND column_name='createdat';
-- SELECT column_name, data_type, udt_name FROM information_schema.columns WHERE table_name='rules' AND column_name IN ('createdat','evaluatedat');
-- SELECT column_name, data_type, udt_name FROM information_schema.columns WHERE table_name='events' AND column_name='timestamp';
-- SELECT column_name, data_type, udt_name FROM information_schema.columns WHERE table_name='submissions' AND column_name='evaluatedat';
-- SELECT column_name, data_type, udt_name FROM information_schema.columns WHERE table_name='carriers' AND column_name='createdat';

-- 3) Convert columns that are currently text (or varchar) but contain parseable timestamps.
-- These ALTER commands assume column values are ISO-like timestamp strings or already timestamps stored in text form.
-- If your values use a non-standard format, you'll need to adjust the USING expression (e.g., to_timestamp(..., 'format')).

-- Users table
ALTER TABLE users ALTER COLUMN createdat TYPE timestamptz USING (createdat::timestamptz);
ALTER TABLE users ALTER COLUMN lastloginat TYPE timestamptz USING (lastloginat::timestamptz);
ALTER TABLE users ALTER COLUMN lockoutend TYPE timestamptz USING (lockoutend::timestamptz);
ALTER TABLE users ALTER COLUMN passwordresetexpiry TYPE timestamptz USING (passwordresetexpiry::timestamptz);

-- Products
ALTER TABLE products ALTER COLUMN createdat TYPE timestamptz USING (createdat::timestamptz);

-- Rules
ALTER TABLE rules ALTER COLUMN createdat TYPE timestamptz USING (createdat::timestamptz);
ALTER TABLE rules ALTER COLUMN evaluatedat TYPE timestamptz USING (evaluatedat::timestamptz);

-- Events
ALTER TABLE events ALTER COLUMN "timestamp" TYPE timestamptz USING ("timestamp"::timestamptz);

-- Submissions
ALTER TABLE submissions ALTER COLUMN evaluatedat TYPE timestamptz USING (evaluatedat::timestamptz);

-- Carriers
ALTER TABLE carriers ALTER COLUMN createdat TYPE timestamptz USING (createdat::timestamptz);

COMMIT;

-- If any ALTER fails due to unparsable values, restore backups and inspect the rows:
-- SELECT * FROM users_backup WHERE createdat IS NOT NULL AND (createdat::text !~ '^\\d{4}-\\d{2}-\\d{2}');
-- Use an appropriate to_timestamp(...) expression to parse custom formats.

-- End of script
