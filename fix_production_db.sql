-- Fix Groups table: Add Source and DisplayColor columns
DO $$ 
BEGIN 
    -- Add Source column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Groups' AND column_name = 'Source'
    ) THEN
        ALTER TABLE "Groups" ADD COLUMN "Source" VARCHAR(50) NOT NULL DEFAULT 'Egen';
        RAISE NOTICE 'Added Source column to Groups';
    ELSE
        RAISE NOTICE 'Source column already exists in Groups';
    END IF;
    
    -- Add DisplayColor column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Groups' AND column_name = 'DisplayColor'
    ) THEN
        ALTER TABLE "Groups" ADD COLUMN "DisplayColor" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå';
        RAISE NOTICE 'Added DisplayColor column to Groups';
    ELSE
        RAISE NOTICE 'DisplayColor column already exists in Groups';
    END IF;
END $$;

-- Update existing groups with default values if NULL
UPDATE "Groups" 
SET "Source" = COALESCE("Source", 'Egen'),
    "DisplayColor" = COALESCE("DisplayColor", 'Ljusblå')
WHERE "Source" IS NULL OR "DisplayColor" IS NULL;

-- Fix GroupTypes table: Add StandardDisplayColor column
DO $$ 
BEGIN 
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'GroupTypes' AND column_name = 'StandardDisplayColor'
    ) THEN
        ALTER TABLE "GroupTypes" ADD COLUMN "StandardDisplayColor" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå';
        RAISE NOTICE 'Added StandardDisplayColor column to GroupTypes';
    ELSE
        RAISE NOTICE 'StandardDisplayColor column already exists in GroupTypes';
    END IF;
END $$;

-- Update existing GroupTypes with default values if NULL
UPDATE "GroupTypes" 
SET "StandardDisplayColor" = COALESCE("StandardDisplayColor", 'Ljusblå')
WHERE "StandardDisplayColor" IS NULL;

SELECT '✅ All columns fixed!' as status;

