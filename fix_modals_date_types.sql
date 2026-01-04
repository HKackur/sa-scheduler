-- Fix Modals table: Convert TEXT columns to DATE/TIMESTAMP
-- This script updates existing Modals and ModalReadBy tables to use proper date types

-- First, check and convert Modals table
DO $$ 
BEGIN 
    -- Convert StartDate from TEXT to DATE
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'Modals' 
        AND column_name = 'StartDate' 
        AND data_type = 'text'
    ) THEN
        ALTER TABLE "Modals" 
        ALTER COLUMN "StartDate" TYPE DATE USING "StartDate"::DATE;
        RAISE NOTICE 'Converted Modals.StartDate from TEXT to DATE';
    END IF;
    
    -- Convert EndDate from TEXT to DATE
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'Modals' 
        AND column_name = 'EndDate' 
        AND data_type = 'text'
    ) THEN
        ALTER TABLE "Modals" 
        ALTER COLUMN "EndDate" TYPE DATE USING "EndDate"::DATE;
        RAISE NOTICE 'Converted Modals.EndDate from TEXT to DATE';
    END IF;
    
    -- Convert CreatedAt from TEXT to TIMESTAMP
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'Modals' 
        AND column_name = 'CreatedAt' 
        AND data_type = 'text'
    ) THEN
        ALTER TABLE "Modals" 
        ALTER COLUMN "CreatedAt" TYPE TIMESTAMP USING "CreatedAt"::TIMESTAMP;
        RAISE NOTICE 'Converted Modals.CreatedAt from TEXT to TIMESTAMP';
    END IF;
    
    -- Convert UpdatedAt from TEXT to TIMESTAMP
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'Modals' 
        AND column_name = 'UpdatedAt' 
        AND data_type = 'text'
    ) THEN
        ALTER TABLE "Modals" 
        ALTER COLUMN "UpdatedAt" TYPE TIMESTAMP USING "UpdatedAt"::TIMESTAMP;
        RAISE NOTICE 'Converted Modals.UpdatedAt from TEXT to TIMESTAMP';
    END IF;
END $$;

-- Convert ModalReadBy table
DO $$ 
BEGIN 
    -- Convert ReadAt from TEXT to TIMESTAMP
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'ModalReadBy' 
        AND column_name = 'ReadAt' 
        AND data_type = 'text'
    ) THEN
        ALTER TABLE "ModalReadBy" 
        ALTER COLUMN "ReadAt" TYPE TIMESTAMP USING "ReadAt"::TIMESTAMP;
        RAISE NOTICE 'Converted ModalReadBy.ReadAt from TEXT to TIMESTAMP';
    END IF;
END $$;

-- Verify the changes
SELECT 
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_schema = 'public' 
AND table_name IN ('Modals', 'ModalReadBy')
AND column_name IN ('StartDate', 'EndDate', 'CreatedAt', 'UpdatedAt', 'ReadAt')
ORDER BY table_name, column_name;

