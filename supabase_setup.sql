-- Run this in Supabase SQL Editor to create all tables
-- PostgreSQL version

-- ============================================
-- Identity Tables (ApplicationDbContext)
-- ============================================

CREATE TABLE IF NOT EXISTS "AspNetRoles" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT,
    "NormalizedName" TEXT,
    "ConcurrencyStamp" TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE TABLE IF NOT EXISTS "AspNetUsers" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "UserName" TEXT,
    "NormalizedUserName" TEXT,
    "Email" TEXT,
    "NormalizedEmail" TEXT,
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "ConcurrencyStamp" TEXT,
    "PhoneNumber" TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMPTZ,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AccessFailedCount" INTEGER NOT NULL DEFAULT 0,
    "LastLoginAt" TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- Migration History Table for Identity (in public schema)
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

-- ============================================
-- App Tables (AppDbContext)
-- ============================================

CREATE TABLE IF NOT EXISTS "Places" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "DefaultDurationMin" INTEGER NOT NULL,
    "SnapMin" INTEGER NOT NULL,
    "VisibleStartMin" INTEGER NOT NULL,
    "VisibleEndMin" INTEGER NOT NULL,
    "UserId" TEXT
);

CREATE TABLE IF NOT EXISTS "Areas" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Path" TEXT NOT NULL,
    "PlaceId" TEXT NOT NULL,
    "ParentAreaId" TEXT,
    "Level1WidthPercent" INTEGER NOT NULL,
    "Level2WidthPercent" INTEGER NOT NULL,
    "Level3WidthPercent" INTEGER NOT NULL,
    CONSTRAINT "FK_Areas_Places_PlaceId" FOREIGN KEY ("PlaceId") REFERENCES "Places" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Areas_Areas_ParentAreaId" FOREIGN KEY ("ParentAreaId") REFERENCES "Areas" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_Areas_PlaceId" ON "Areas" ("PlaceId");
CREATE INDEX IF NOT EXISTS "IX_Areas_ParentAreaId" ON "Areas" ("ParentAreaId");
CREATE INDEX IF NOT EXISTS "IX_Areas_Path" ON "Areas" ("Path");

CREATE TABLE IF NOT EXISTS "Leafs" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "PlaceId" TEXT NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    CONSTRAINT "FK_Leafs_Places_PlaceId" FOREIGN KEY ("PlaceId") REFERENCES "Places" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Leafs_PlaceId" ON "Leafs" ("PlaceId");

CREATE TABLE IF NOT EXISTS "AreaLeafs" (
    "AreaId" TEXT NOT NULL,
    "LeafId" TEXT NOT NULL,
    CONSTRAINT "PK_AreaLeafs" PRIMARY KEY ("AreaId", "LeafId"),
    CONSTRAINT "FK_AreaLeafs_Areas_AreaId" FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AreaLeafs_Leafs_LeafId" FOREIGN KEY ("LeafId") REFERENCES "Leafs" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AreaLeafs_AreaId" ON "AreaLeafs" ("AreaId");
CREATE INDEX IF NOT EXISTS "IX_AreaLeafs_LeafId" ON "AreaLeafs" ("LeafId");

CREATE TABLE IF NOT EXISTS "Groups" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "ColorHex" TEXT NOT NULL,
    "GroupType" TEXT,
    "UserId" TEXT
);

CREATE TABLE IF NOT EXISTS "GroupTypes" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "UserId" TEXT
);

CREATE TABLE IF NOT EXISTS "ScheduleTemplates" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "PlaceId" TEXT NOT NULL,
    "UserId" TEXT,
    CONSTRAINT "FK_ScheduleTemplates_Places_PlaceId" FOREIGN KEY ("PlaceId") REFERENCES "Places" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_ScheduleTemplates_PlaceId" ON "ScheduleTemplates" ("PlaceId");

CREATE TABLE IF NOT EXISTS "BookingTemplates" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "ScheduleTemplateId" TEXT NOT NULL,
    "AreaId" TEXT NOT NULL,
    "GroupId" TEXT NOT NULL,
    "DayOfWeek" INTEGER NOT NULL,
    "StartMin" INTEGER NOT NULL,
    "EndMin" INTEGER NOT NULL,
    "Notes" TEXT,
    "ContactName" TEXT,
    "ContactEmail" TEXT,
    "ContactPhone" TEXT,
    "CreatedAt" TIMESTAMPTZ,
    "UpdatedAt" TIMESTAMPTZ,
    CONSTRAINT "FK_BookingTemplates_ScheduleTemplates_ScheduleTemplateId" FOREIGN KEY ("ScheduleTemplateId") REFERENCES "ScheduleTemplates" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BookingTemplates_Areas_AreaId" FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BookingTemplates_Groups_GroupId" FOREIGN KEY ("GroupId") REFERENCES "Groups" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_BookingTemplates_AreaId" ON "BookingTemplates" ("AreaId");
CREATE INDEX IF NOT EXISTS "IX_BookingTemplates_GroupId" ON "BookingTemplates" ("GroupId");
CREATE INDEX IF NOT EXISTS "IX_BookingTemplates_ScheduleTemplateId_DayOfWeek_StartMin" ON "BookingTemplates" ("ScheduleTemplateId", "DayOfWeek", "StartMin");

CREATE TABLE IF NOT EXISTS "Bookings" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "AreaId" TEXT NOT NULL,
    "GroupId" TEXT NOT NULL,
    "Date" DATE NOT NULL,
    "StartMin" INTEGER NOT NULL,
    "EndMin" INTEGER NOT NULL,
    "Notes" TEXT,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_Bookings_Areas_AreaId" FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Bookings_Groups_GroupId" FOREIGN KEY ("GroupId") REFERENCES "Groups" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Bookings_AreaId" ON "Bookings" ("AreaId");
CREATE INDEX IF NOT EXISTS "IX_Bookings_GroupId" ON "Bookings" ("GroupId");

CREATE TABLE IF NOT EXISTS "CalendarBookings" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "AreaId" TEXT NOT NULL,
    "GroupId" TEXT NOT NULL,
    "Date" DATE NOT NULL,
    "StartMin" INTEGER NOT NULL,
    "EndMin" INTEGER NOT NULL,
    "Notes" TEXT,
    "ContactName" TEXT,
    "ContactEmail" TEXT,
    "ContactPhone" TEXT,
    "SourceTemplateId" TEXT,
    "CreatedAt" TIMESTAMPTZ,
    "UpdatedAt" TIMESTAMPTZ,
    CONSTRAINT "FK_CalendarBookings_Areas_AreaId" FOREIGN KEY ("AreaId") REFERENCES "Areas" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CalendarBookings_Groups_GroupId" FOREIGN KEY ("GroupId") REFERENCES "Groups" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CalendarBookings_ScheduleTemplates_SourceTemplateId" FOREIGN KEY ("SourceTemplateId") REFERENCES "ScheduleTemplates" ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_CalendarBookings_AreaId" ON "CalendarBookings" ("AreaId");
CREATE INDEX IF NOT EXISTS "IX_CalendarBookings_GroupId" ON "CalendarBookings" ("GroupId");
CREATE INDEX IF NOT EXISTS "IX_CalendarBookings_SourceTemplateId" ON "CalendarBookings" ("SourceTemplateId");
