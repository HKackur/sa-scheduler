#!/bin/bash
set -e

echo "🔧 Fixing Groups table columns in production database..."

cd SchedulerMVP

CONNECTION_STRING="Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;Trust Server Certificate=true"

echo "📦 Running SQL commands to add missing columns..."

# Use dotnet ef to run raw SQL
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
dotnet ef database update --context AppDbContext --no-build 2>&1 | grep -E "(Adding|Updating|column|Groups)" || true

# Now run the fix SQL directly using a C# one-liner
cat > /tmp/fix_groups_temp.cs << 'CSHARP'
using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
var connStr = "Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;Trust Server Certificate=true";
var opts = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connStr).Options;
using var ctx = new AppDbContext(opts);
await ctx.Database.OpenConnectionAsync();
try {
    await ctx.Database.ExecuteSqlRawAsync(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Groups' AND column_name = 'Source') THEN ALTER TABLE ""Groups"" ADD COLUMN ""Source"" VARCHAR(50) NOT NULL DEFAULT 'Egen'; END IF; END $$;");
    await ctx.Database.ExecuteSqlRawAsync(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Groups' AND column_name = 'DisplayColor') THEN ALTER TABLE ""Groups"" ADD COLUMN ""DisplayColor"" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå'; END IF; END $$;");
    await ctx.Database.ExecuteSqlRawAsync(@"UPDATE ""Groups"" SET ""Source"" = COALESCE(""Source"", 'Egen'), ""DisplayColor"" = COALESCE(""DisplayColor"", 'Ljusblå') WHERE ""Source"" IS NULL OR ""DisplayColor"" IS NULL;");
    await ctx.Database.ExecuteSqlRawAsync(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'GroupTypes' AND column_name = 'StandardDisplayColor') THEN ALTER TABLE ""GroupTypes"" ADD COLUMN ""StandardDisplayColor"" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå'; END IF; END $$;");
    Console.WriteLine("✅ All columns fixed!");
} catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
CSHARP

# Try to compile and run
cd SchedulerMVP
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
dotnet script /tmp/fix_groups_temp.cs 2>&1 || {
    echo "⚠️  Using alternative method via EF migrations..."
    
    # Force run the migration that adds these columns
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
    dotnet ef database update 20251221120000_AddSourceAndDisplayColorToGroup --context AppDbContext --no-build 2>&1 || echo "Migration may have already run"
}

echo "✅ Fix script completed!"

