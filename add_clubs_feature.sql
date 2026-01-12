-- SQL Script to add Clubs feature to Supabase database
-- Run this in Supabase SQL Editor

-- Step 1: Create Clubs table
-- Note: Using TEXT for Id to match EF Core configuration (value converters handle GUID <-> TEXT)
CREATE TABLE IF NOT EXISTS "Clubs" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMPTZ,
    "UpdatedAt" TIMESTAMPTZ
);

-- Step 2: Add ClubId column to AspNetUsers (Identity table)
ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "ClubId" TEXT;

-- Step 3: Add ClubId columns to application tables
-- Note: Using TEXT to match EF Core configuration (value converters handle GUID <-> TEXT)
ALTER TABLE "Places" 
ADD COLUMN IF NOT EXISTS "ClubId" TEXT;

ALTER TABLE "Groups" 
ADD COLUMN IF NOT EXISTS "ClubId" TEXT;

ALTER TABLE "ScheduleTemplates" 
ADD COLUMN IF NOT EXISTS "ClubId" TEXT;

ALTER TABLE "GroupTypes" 
ADD COLUMN IF NOT EXISTS "ClubId" TEXT;

-- Step 4: Create indexes for ClubId columns
CREATE INDEX IF NOT EXISTS "IX_Places_ClubId" ON "Places"("ClubId");
CREATE INDEX IF NOT EXISTS "IX_Groups_ClubId" ON "Groups"("ClubId");
CREATE INDEX IF NOT EXISTS "IX_ScheduleTemplates_ClubId" ON "ScheduleTemplates"("ClubId");
CREATE INDEX IF NOT EXISTS "IX_GroupTypes_ClubId" ON "GroupTypes"("ClubId");

-- Step 5: Create foreign key constraints (ON DELETE SET NULL)
-- Note: AspNetUsers.ClubId does NOT have a foreign key because 
-- ApplicationUser is in ApplicationDbContext and Club is in AppDbContext

ALTER TABLE "Places"
DROP CONSTRAINT IF EXISTS "FK_Places_Clubs_ClubId";

ALTER TABLE "Places"
ADD CONSTRAINT "FK_Places_Clubs_ClubId" 
FOREIGN KEY ("ClubId") 
REFERENCES "Clubs"("Id") 
ON DELETE SET NULL;

ALTER TABLE "Groups"
DROP CONSTRAINT IF EXISTS "FK_Groups_Clubs_ClubId";

ALTER TABLE "Groups"
ADD CONSTRAINT "FK_Groups_Clubs_ClubId" 
FOREIGN KEY ("ClubId") 
REFERENCES "Clubs"("Id") 
ON DELETE SET NULL;

ALTER TABLE "ScheduleTemplates"
DROP CONSTRAINT IF EXISTS "FK_ScheduleTemplates_Clubs_ClubId";

ALTER TABLE "ScheduleTemplates"
ADD CONSTRAINT "FK_ScheduleTemplates_Clubs_ClubId" 
FOREIGN KEY ("ClubId") 
REFERENCES "Clubs"("Id") 
ON DELETE SET NULL;

ALTER TABLE "GroupTypes"
DROP CONSTRAINT IF EXISTS "FK_GroupTypes_Clubs_ClubId";

ALTER TABLE "GroupTypes"
ADD CONSTRAINT "FK_GroupTypes_Clubs_ClubId" 
FOREIGN KEY ("ClubId") 
REFERENCES "Clubs"("Id") 
ON DELETE SET NULL;

-- Verification queries (run these after the script to verify):
-- SELECT * FROM "Clubs" LIMIT 1;
-- SELECT "ClubId" FROM "AspNetUsers" LIMIT 1;
-- SELECT "ClubId" FROM "Places" LIMIT 1;
-- SELECT "ClubId" FROM "Groups" LIMIT 1;
-- SELECT "ClubId" FROM "ScheduleTemplates" LIMIT 1;
-- SELECT "ClubId" FROM "GroupTypes" LIMIT 1;

