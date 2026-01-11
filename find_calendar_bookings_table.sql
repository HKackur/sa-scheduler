-- First, let's find what tables actually exist
-- Run this first to see the actual table names

SELECT 
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'public' 
ORDER BY table_name;

-- Also check for any table with "booking" in the name (case insensitive)
SELECT 
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'public' 
    AND table_name ILIKE '%booking%'
ORDER BY table_name;

-- Check columns in CalendarBookings if it exists
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'public' 
    AND table_name ILIKE '%calendarbooking%'
ORDER BY ordinal_position;



