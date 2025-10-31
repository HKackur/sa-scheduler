-- Create tables in Supabase
CREATE TABLE IF NOT EXISTS "Groups" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "ColorHex" TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS "Places" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "DefaultDurationMin" INTEGER NOT NULL,
    "SnapMin" INTEGER NOT NULL,
    "VisibleStartMin" INTEGER NOT NULL,
    "VisibleEndMin" INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS "Areas" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "PlaceId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "ParentAreaId" TEXT NULL,
    "Path" TEXT NOT NULL,
    "Level1WidthPercent" INTEGER NOT NULL DEFAULT 0,
    "Level2WidthPercent" INTEGER NOT NULL DEFAULT 0,
    "Level3WidthPercent" INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY ("ParentAreaId") REFERENCES "Areas" ("Id") ON DELETE RESTRICT,
    FOREIGN KEY ("PlaceId") REFERENCES "Places" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "Leafs" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "PlaceId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    FOREIGN KEY ("PlaceId") REFERENCES "Places" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "ScheduleTemplates" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "PlaceId" TEXT NOT NULL,
    FOREIGN KEY ("PlaceId") REFERENCES "Places" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AreaLeafs" (
    "AreaId" TEXT NOT NULL,
    "LeafId" TEXT NOT NULL,
    PRIMARY KEY ("AreaId", "LeafId"),
    FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE CASCADE,
    FOREIGN KEY ("LeafId") REFERENCES "Leafs" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "BookingTemplates" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "ScheduleTemplateId" TEXT NOT NULL,
    "AreaId" TEXT NOT NULL,
    "GroupId" TEXT NOT NULL,
    "DayOfWeek" INTEGER NOT NULL,
    "StartMin" INTEGER NOT NULL,
    "EndMin" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE CASCADE,
    FOREIGN KEY ("GroupId") REFERENCES "Groups" ("Id") ON DELETE CASCADE,
    FOREIGN KEY ("ScheduleTemplateId") REFERENCES "ScheduleTemplates" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "Bookings" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "AreaId" TEXT NOT NULL,
    "GroupId" TEXT NOT NULL,
    "Date" TEXT NOT NULL,
    "StartMin" INTEGER NOT NULL,
    "EndMin" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE CASCADE,
    FOREIGN KEY ("GroupId") REFERENCES "Groups" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "CalendarBookings" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "AreaId" TEXT NOT NULL,
    "GroupId" TEXT NOT NULL,
    "Date" TEXT NOT NULL,
    "StartMin" INTEGER NOT NULL,
    "EndMin" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    "SourceTemplateId" TEXT NULL,
    FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE CASCADE,
    FOREIGN KEY ("SourceTemplateId") REFERENCES "BookingTemplates" ("Id"),
    FOREIGN KEY ("GroupId") REFERENCES "Groups" ("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX "IX_AreaLeafs_AreaId" ON "AreaLeafs" ("AreaId");
CREATE INDEX "IX_AreaLeafs_LeafId" ON "AreaLeafs" ("LeafId");
CREATE INDEX "IX_Areas_ParentAreaId" ON "Areas" ("ParentAreaId");
CREATE INDEX "IX_Areas_Path" ON "Areas" ("Path");
CREATE INDEX "IX_Areas_PlaceId" ON "Areas" ("PlaceId");
CREATE INDEX "IX_BookingTemplates_AreaId" ON "BookingTemplates" ("AreaId");
CREATE INDEX "IX_BookingTemplates_GroupId" ON "BookingTemplates" ("GroupId");
CREATE INDEX "IX_BookingTemplates_ScheduleTemplateId_DayOfWeek_StartMin" ON "BookingTemplates" ("ScheduleTemplateId", "DayOfWeek", "StartMin");
CREATE INDEX "IX_Leafs_PlaceId" ON "Leafs" ("PlaceId");
CREATE INDEX "IX_ScheduleTemplates_PlaceId" ON "ScheduleTemplates" ("PlaceId");
CREATE INDEX "IX_Bookings_AreaId" ON "Bookings" ("AreaId");
CREATE INDEX "IX_Bookings_GroupId" ON "Bookings" ("GroupId");
CREATE INDEX "IX_CalendarBookings_AreaId" ON "CalendarBookings" ("AreaId");
CREATE INDEX "IX_CalendarBookings_GroupId" ON "CalendarBookings" ("GroupId");
CREATE INDEX "IX_CalendarBookings_SourceTemplateId" ON "CalendarBookings" ("SourceTemplateId");
