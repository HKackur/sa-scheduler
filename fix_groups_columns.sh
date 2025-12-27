#!/bin/bash
set -e

echo "🔧 Fixing Groups table columns in production database..."

cd SchedulerMVP

CONNECTION_STRING="Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;Trust Server Certificate=true"

echo "📦 Adding Source and DisplayColor columns to Groups table (if missing)..."

# Create a temporary C# script to run the SQL commands
cat > /tmp/fix_groups_columns.cs << 'EOF'
using Npgsql;
using System;

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
    ?? "Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;Trust Server Certificate=true";

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

// Check if Source column exists
var checkSourceCmd = new NpgsqlCommand(@"
    SELECT column_name 
    FROM information_schema.columns 
    WHERE table_name = 'Groups' AND column_name = 'Source'
", conn);
var sourceExists = await checkSourceCmd.ExecuteScalarAsync() != null;

// Check if DisplayColor column exists
var checkDisplayColorCmd = new NpgsqlCommand(@"
    SELECT column_name 
    FROM information_schema.columns 
    WHERE table_name = 'Groups' AND column_name = 'DisplayColor'
", conn);
var displayColorExists = await checkDisplayColorCmd.ExecuteScalarAsync() != null;

if (!sourceExists)
{
    Console.WriteLine("Adding Source column...");
    var addSourceCmd = new NpgsqlCommand(@"
        ALTER TABLE ""Groups"" 
        ADD COLUMN ""Source"" VARCHAR(50) NOT NULL DEFAULT 'Egen'
    ", conn);
    await addSourceCmd.ExecuteNonQueryAsync();
    Console.WriteLine("✅ Source column added");
}
else
{
    Console.WriteLine("Source column already exists");
}

if (!displayColorExists)
{
    Console.WriteLine("Adding DisplayColor column...");
    var addDisplayColorCmd = new NpgsqlCommand(@"
        ALTER TABLE ""Groups"" 
        ADD COLUMN ""DisplayColor"" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå'
    ", conn);
    await addDisplayColorCmd.ExecuteNonQueryAsync();
    Console.WriteLine("✅ DisplayColor column added");
}
else
{
    Console.WriteLine("DisplayColor column already exists");
}

// Update existing groups with default values if NULL
Console.WriteLine("Updating existing groups with default values...");
var updateCmd = new NpgsqlCommand(@"
    UPDATE ""Groups"" 
    SET ""Source"" = COALESCE(""Source"", 'Egen'),
        ""DisplayColor"" = COALESCE(""DisplayColor"", 'Ljusblå')
    WHERE ""Source"" IS NULL OR ""DisplayColor"" IS NULL
", conn);
var updated = await updateCmd.ExecuteNonQueryAsync();
Console.WriteLine($"✅ Updated {updated} groups");

// Check and add StandardDisplayColor to GroupTypes
var checkStandardDisplayColorCmd = new NpgsqlCommand(@"
    SELECT column_name 
    FROM information_schema.columns 
    WHERE table_name = 'GroupTypes' AND column_name = 'StandardDisplayColor'
", conn);
var standardDisplayColorExists = await checkStandardDisplayColorCmd.ExecuteScalarAsync() != null;

if (!standardDisplayColorExists)
{
    Console.WriteLine("Adding StandardDisplayColor column to GroupTypes...");
    var addStandardDisplayColorCmd = new NpgsqlCommand(@"
        ALTER TABLE ""GroupTypes"" 
        ADD COLUMN ""StandardDisplayColor"" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå'
    ", conn);
    await addStandardDisplayColorCmd.ExecuteNonQueryAsync();
    Console.WriteLine("✅ StandardDisplayColor column added");
}
else
{
    Console.WriteLine("StandardDisplayColor column already exists");
}

Console.WriteLine("✅ All columns fixed!");
EOF

# Run the script using dotnet-script or compile and run
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
CONNECTION_STRING="$CONNECTION_STRING" \
dotnet script /tmp/fix_groups_columns.cs 2>&1 || {
    echo "⚠️  dotnet-script not available, trying alternative method..."
    
    # Alternative: Use psql directly
    echo "Using psql to fix columns..."
    PGPASSWORD="bunch-hiccups-misery-extreme" psql -h db.anebyqfrzsuqwrbncwxt.supabase.co -p 5432 -U postgres -d postgres << 'PSQL'
-- Add Source column if it doesn't exist
DO \$\$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Groups' AND column_name = 'Source'
    ) THEN
        ALTER TABLE "Groups" ADD COLUMN "Source" VARCHAR(50) NOT NULL DEFAULT 'Egen';
    END IF;
END \$\$;

-- Add DisplayColor column if it doesn't exist
DO \$\$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Groups' AND column_name = 'DisplayColor'
    ) THEN
        ALTER TABLE "Groups" ADD COLUMN "DisplayColor" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå';
    END IF;
END \$\$;

-- Update existing groups
UPDATE "Groups" 
SET "Source" = COALESCE("Source", 'Egen'),
    "DisplayColor" = COALESCE("DisplayColor", 'Ljusblå')
WHERE "Source" IS NULL OR "DisplayColor" IS NULL;

-- Add StandardDisplayColor to GroupTypes if it doesn't exist
DO \$\$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'GroupTypes' AND column_name = 'StandardDisplayColor'
    ) THEN
        ALTER TABLE "GroupTypes" ADD COLUMN "StandardDisplayColor" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå';
    END IF;
END \$\$;

SELECT '✅ All columns fixed!' as status;
PSQL
}

echo "✅ Groups table columns fixed!"

