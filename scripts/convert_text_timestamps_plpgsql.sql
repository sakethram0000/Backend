-- convert_text_timestamps_plpgsql.sql
-- Safer Postgres script to convert columns stored as text to timestamptz only when needed.
-- IMPORTANT: Run on Postgres. Backup before running. Test on staging first.
-- Usage: psql "postgresql://user:pass@host:port/dbname" -f convert_text_timestamps_plpgsql.sql

-- List of table -> candidate timestamp columns to convert (snake_case as used by EF).
-- Add or remove columns as needed for your schema.

DO $$
DECLARE
    tbl text;
    col text;
    rec record;
    alter_sql text;
BEGIN
    -- Define pairs to check. Adjust this list if your schema differs.
    FOR rec IN SELECT * FROM (VALUES
        ('users','created_at'),
        ('users','last_login_at'),
        ('users','lockout_end'),
        ('users','password_reset_expiry'),
        ('products','created_at'),
        ('rules','created_at'),
        ('rules','evaluated_at'),
        ('rules','effective_from'),
        ('rules','effective_to'),
        ('rules','updated_at'),
        ('events','timestamp'),
        ('submissions','evaluated_at'),
        ('carriers','created_at')
    ) AS t(tbl, col)
    LOOP
        tbl := rec.tbl;
        col := rec.col;

        -- Check if table & column exist and column is text
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = tbl AND column_name = col AND udt_name = 'text') THEN
            RAISE NOTICE 'Preparing to convert %.% (udt=text)', tbl, col;

            -- Create a backup of the table if not exists (schema + data)
            EXECUTE format('CREATE TABLE IF NOT EXISTS %I_backup AS TABLE %I WITH NO DATA', tbl, tbl);
            EXECUTE format('INSERT INTO %I_backup SELECT * FROM %I', tbl || '_backup', tbl);

            -- Build and run the ALTER statement using USING cast; coalesce empty strings to NULL
            alter_sql := format('ALTER TABLE %I ALTER COLUMN %I TYPE timestamptz USING (NULLIF(trim(%I), '''')::timestamptz)', tbl, col, col);
            RAISE NOTICE 'Executing: %', alter_sql;
            EXECUTE alter_sql;

            RAISE NOTICE 'Converted %.% to timestamptz', tbl, col;
        ELSE
            RAISE NOTICE 'Skipping %.% (not found or not text)', tbl, col;
        END IF;
    END LOOP;
END$$;

-- After running, verify types with:
-- SELECT column_name, data_type, udt_name FROM information_schema.columns WHERE table_name='users';

-- End of script
