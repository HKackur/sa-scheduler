using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchedulerMVP.Data;

// Connection string for production Supabase database
var connectionString = "Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;Trust Server Certificate=true";

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql(connectionString);

using var context = new AppDbContext(optionsBuilder.Options);
await context.Database.OpenConnectionAsync();

Console.WriteLine("🔧 Fixing Groups table columns in production database...");

// Check if Source column exists
var checkSourceSql = @"
    SELECT column_name 
    FROM information_schema.columns 
    WHERE table_name = 'Groups' AND column_name = 'Source'";
var sourceExists = await context.Database.ExecuteSqlRawAsync($"SELECT EXISTS({checkSourceSql})") != null;

// Check if DisplayColor column exists  
var checkDisplayColorSql = @"
    SELECT column_name 
    FROM information_schema.columns 
    WHERE table_name = 'Groups' AND column_name = 'DisplayColor'";
var displayColorExists = await context.Database.ExecuteSqlRawAsync($"SELECT EXISTS({checkDisplayColorSql})") != null;

if (!sourceExists)
{
    Console.WriteLine("Adding Source column...");
    await context.Database.ExecuteSqlRawAsync(@"
        ALTER TABLE ""Groups"" 
        ADD COLUMN ""Source"" VARCHAR(50) NOT NULL DEFAULT 'Egen'
    ");
    Console.WriteLine("✅ Source column added");
}
else
{
    Console.WriteLine("Source column already exists");
}

if (!displayColorExists)
{
    Console.WriteLine("Adding DisplayColor column...");
    await context.Database.ExecuteSqlRawAsync(@"
        ALTER TABLE ""Groups"" 
        ADD COLUMN ""DisplayColor"" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå'
    ");
    Console.WriteLine("✅ DisplayColor column added");
}
else
{
    Console.WriteLine("DisplayColor column already exists");
}

// Update existing groups with default values if NULL
Console.WriteLine("Updating existing groups with default values...");
var updateResult = await context.Database.ExecuteSqlRawAsync(@"
    UPDATE ""Groups"" 
    SET ""Source"" = COALESCE(""Source"", 'Egen'),
        ""DisplayColor"" = COALESCE(""DisplayColor"", 'Ljusblå')
    WHERE ""Source"" IS NULL OR ""DisplayColor"" IS NULL
");
Console.WriteLine($"✅ Updated {updateResult} groups");

// Check and add StandardDisplayColor to GroupTypes
var checkStandardDisplayColorSql = @"
    SELECT column_name 
    FROM information_schema.columns 
    WHERE table_name = 'GroupTypes' AND column_name = 'StandardDisplayColor'";
var standardDisplayColorExists = await context.Database.ExecuteSqlRawAsync($"SELECT EXISTS({checkStandardDisplayColorSql})") != null;

if (!standardDisplayColorExists)
{
    Console.WriteLine("Adding StandardDisplayColor column to GroupTypes...");
    await context.Database.ExecuteSqlRawAsync(@"
        ALTER TABLE ""GroupTypes"" 
        ADD COLUMN ""StandardDisplayColor"" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå'
    ");
    Console.WriteLine("✅ StandardDisplayColor column added");
}
else
{
    Console.WriteLine("StandardDisplayColor column already exists");
}

Console.WriteLine("✅ All columns fixed!");

