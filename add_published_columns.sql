-- Add Published and PublishedAt columns to CalendarBookings table
-- This fixes the error: "column c.Published does not exist"
-- 
-- Run this in Supabase SQL Editor

-- First, let's find the actual table name (case-sensitive in PostgreSQL)
DO $$ 
DECLARE
    actual_table_name text;
BEGIN
    -- Find the table - PostgreSQL stores names in lowercase in information_schema
    -- unless they were created with quotes
    SELECT table_name INTO actual_table_name
    FROM information_schema.tables
    WHERE table_schema = 'public' 
        AND (table_name = 'CalendarBookings' OR LOWER(table_name) = 'calendarbookings');
    
    IF actual_table_name IS NULL THEN
        RAISE EXCEPTION 'Table CalendarBookings not found in public schema. Available tables: %', 
            (SELECT string_agg(table_name, ', ') FROM information_schema.tables WHERE table_schema = 'public');
    END IF;
    
    RAISE NOTICE 'Found table: %', actual_table_name;
    
    -- Add Published column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = actual_table_name
        AND column_name = 'Published'
    ) THEN
        EXECUTE format('ALTER TABLE "public"."%s" ADD COLUMN "Published" BOOLEAN NOT NULL DEFAULT FALSE', actual_table_name);
        RAISE NOTICE 'Added Published column to %', actual_table_name;
    ELSE
        RAISE NOTICE 'Published column already exists in %', actual_table_name;
    END IF;

    -- Add PublishedAt column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = actual_table_name
        AND column_name = 'PublishedAt'
    ) THEN
        EXECUTE format('ALTER TABLE "public"."%s" ADD COLUMN "PublishedAt" TIMESTAMPTZ NULL', actual_table_name);
        RAISE NOTICE 'Added PublishedAt column to %', actual_table_name;
    ELSE
        RAISE NOTICE 'PublishedAt column already exists in %', actual_table_name;
    END IF;
END $$;

-- List all tables to help debug (if needed)
SELECT 
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'public' 
    AND table_name ILIKE '%booking%'
ORDER BY table_name;

-- Verify the columns were added
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'public' 
    AND table_name ILIKE '%calendarbooking%'
    AND column_name IN ('Published', 'PublishedAt')
ORDER BY table_name, column_name;

