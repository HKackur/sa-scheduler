-- SQL script för att skapa SharedScheduleLinks tabellen i Supabase/PostgreSQL
-- Kör detta i Supabase SQL Editor direkt

-- Skapa SharedScheduleLinks tabellen
CREATE TABLE IF NOT EXISTS "SharedScheduleLinks" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "ScheduleTemplateId" TEXT NOT NULL,
    "ShareToken" TEXT NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "AllowWeekView" BOOLEAN NOT NULL DEFAULT true,
    "AllowDayView" BOOLEAN NOT NULL DEFAULT false,
    "AllowListView" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LastAccessedAt" TIMESTAMP NULL,
    "AllowBookingRequests" BOOLEAN NOT NULL DEFAULT false,
    CONSTRAINT "FK_SharedScheduleLinks_ScheduleTemplates_ScheduleTemplateId" 
        FOREIGN KEY ("ScheduleTemplateId") 
        REFERENCES "ScheduleTemplates"("Id") 
        ON DELETE CASCADE
);

-- Skapa unikt index på ShareToken för snabb lookup
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SharedScheduleLinks_ShareToken" 
    ON "SharedScheduleLinks"("ShareToken");

-- Skapa index på ScheduleTemplateId för snabb lookup
CREATE INDEX IF NOT EXISTS "IX_SharedScheduleLinks_ScheduleTemplateId" 
    ON "SharedScheduleLinks"("ScheduleTemplateId");

-- Verifiera att tabellen är skapad
SELECT * FROM "SharedScheduleLinks" LIMIT 1;
