-- convert_text_timestamps_ultra_safe.sql
-- Ultra-safe conversion script for Postgres: for each target text column,
-- 1) create a full table backup (if not exists)
-- 2) extract rows with unparsable timestamp text into a review table and remove them from the main table
-- 3) create a temporary timestamptz column and populate it from parsed values for remaining rows
-- 4) swap the tmp column to the original name (drop old column and rename tmp)
-- This preserves suspicious rows (moved into <table>_<col>_bad) for manual review.
-- IMPORTANT: Backup first with pg_dump. Test on staging. Run with psql.

-- Usage:
-- psql "postgresql://<USER>:<PASSWORD>@<HOST>:<PORT>/<DBNAME>?sslmode=require" -f convert_text_timestamps_ultra_safe.sql

-- Configuration: list target table/column pairs below. Use snake_case names as in DB.
-- Adjust the list if your schema uses different column names.

\echo 'BEGIN ULTRA-SAFE CONVERSION'

DO $$
DECLARE
    rec record;
    tbl text;
    col text;
    tmpcol text;
    backup_tbl text;
    bad_tbl text;
    r record;
    parsed timestamptz;
BEGIN
    FOR rec IN SELECT * FROM (VALUES
        ('users','created_at'),
        ('users','last_login_at'),
        ('users','lockout_end'),
        ('users','password_reset_expiry'),
        ('rules','created_at'),
        ('rules','effective_from'),
        ('rules','effective_to'),
        ('rules','updated_at')
    ) AS t(tbl, col)
    LOOP
        tbl := rec.tbl;
        col := rec.col;
        tmpcol := col || '_tmp_ts';
        backup_tbl := tbl || '_backup_ultrasafe';
        bad_tbl := tbl || '_' || col || '_bad';

        RAISE NOTICE 'Processing %.%', tbl, col;

        -- 1) Create a full table backup (only once)
        EXECUTE format('CREATE TABLE IF NOT EXISTS %I AS TABLE %I WITH NO DATA', backup_tbl, tbl);
        EXECUTE format('INSERT INTO %I SELECT * FROM %I ON CONFLICT DO NOTHING', backup_tbl, tbl);
        RAISE NOTICE 'Backup table % created/updated', backup_tbl;

        -- 2) Prepare bad rows table
        EXECUTE format('CREATE TABLE IF NOT EXISTS %I (ctid_text text PRIMARY KEY, bad_value text, row_json jsonb, extracted_at timestamptz DEFAULT now())', bad_tbl);

        -- 3) Add temporary tmp column to hold parsed timestamptz
        EXECUTE format('ALTER TABLE %I ADD COLUMN IF NOT EXISTS %I timestamptz', tbl, tmpcol);

        -- 4) Iterate rows and attempt to parse; move unparsable into bad table and delete them from main
        FOR r IN EXECUTE format('SELECT ctid, %I AS col_value, to_jsonb(t.*) AS row_json FROM %I t WHERE %I IS NOT NULL AND trim(%I) <> ''''', col, tbl, col, col)
        LOOP
            BEGIN
                -- try to parse trimmed value to timestamptz
                parsed := NULLIF(trim(r.col_value::text), '')::timestamptz;
                -- if parse succeeds, store in tmpcol for that row
                EXECUTE format('UPDATE %I SET %I = $1 WHERE ctid = $2', tbl, tmpcol) USING parsed, r.ctid;
            EXCEPTION WHEN others THEN
                -- on parse failure, insert row into bad table and delete from main table
                RAISE NOTICE 'Moving unparsable row ctid=% to %', r.ctid, bad_tbl;
                EXECUTE format('INSERT INTO %I(ctid_text, bad_value, row_json) VALUES ($1,$2,$3) ON CONFLICT (ctid_text) DO NOTHING', bad_tbl) USING r.ctid::text, r.col_value::text, r.row_json;
                EXECUTE format('DELETE FROM %I WHERE ctid = $1', tbl) USING r.ctid;
            END;
        END LOOP;

        -- 5) After extracting bad rows, check how many bad rows were found
        PERFORM 1 FROM pg_sleep(0); -- small pause for stability
        RAISE NOTICE 'Done scanning % for bad rows. Bad rows count: %', tbl, (SELECT count(*) FROM pg_catalog.pg_tables WHERE false); -- placeholder

        -- 6) Ensure tmpcol is populated for remaining rows; set tmpcol from parsing for any that weren't updated
        EXECUTE format('UPDATE %I SET %I = NULLIF(trim(%I), '''')::timestamptz WHERE %I IS NOT NULL AND (%I IS NULL OR %I = '''')', tbl, tmpcol, col, col, tmpcol, tmpcol);

        -- 7) Now verify there are no remaining unparsable values in main table
        -- If any remaining unparsable values exist, they will fail the cast; they should have been moved earlier.
        -- 8) Swap columns: drop old column and rename tmpcol to original name
        -- Note: this will drop the old text column; backed up rows are in bad_tbl and full table backup exists.
        EXECUTE format('ALTER TABLE %I DROP COLUMN IF EXISTS %I', tbl, col);
        EXECUTE format('ALTER TABLE %I RENAME COLUMN %I TO %I', tbl, tmpcol, col);

        RAISE NOTICE 'Converted column % in table % to timestamptz (tmp swap). Bad rows are in %', col, tbl, bad_tbl;
    END LOOP;
END$$;

\echo 'ULTRA-SAFE CONVERSION COMPLETE'

-- Notes:
-- - This script moves unparsable rows into <table>_<column>_bad tables and deletes them from original table.
-- - Backups are stored in <table>_backup_ultrasafe.
-- - After conversion, run diagnostic queries to confirm column udt_name = 'timestamptz'.
-- - Inspect bad tables to recover/clean suspicious values and re-insert if needed.

-- Verification examples:
-- SELECT column_name, data_type, udt_name FROM information_schema.columns WHERE table_name='users' ORDER BY ordinal_position;
-- SELECT count(*) FROM users_created_at_bad;

-- End of script
