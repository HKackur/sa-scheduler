# Deployment av SharedScheduleLinks funktionen

## Problem
Tabellen `SharedScheduleLinks` finns inte i Supabase-databasen i produktion, vilket gör att delningsfunktionen inte fungerar.

## Lösning: Kör migrationen i produktion

### Alternativ 1: Kör SQL direkt i Supabase SQL Editor (SNABBAST)

1. Öppna Supabase Dashboard: https://supabase.com/dashboard
2. Gå till ditt projekt och öppna "SQL Editor"
3. Kör följande SQL:

```sql
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
```

4. Verifiera att tabellen är skapad:
```sql
SELECT * FROM "SharedScheduleLinks" LIMIT 1;
```

### Alternativ 2: Kör migrations via dotnet ef (AUTOMATISK)

Om du har tillgång till connection string i produktion:

```bash
cd SchedulerMVP

# Uppdatera connection string i run_migrations.sh först!
# Sedan kör:
./run_migrations.sh
```

ELLER manuellt:

```bash
cd SchedulerMVP

# Uppdatera connection string
CONNECTION_STRING="Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<DITT_LÖSENORD>;SSL Mode=Require;"

# Kör migration för AppDbContext
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
dotnet ef database update --context AppDbContext
```

### Alternativ 3: Vänta på automatisk skapelse (EFTER deploy)

Program.cs har nu kod som automatiskt skapar tabellen vid app-start om den inte finns. Efter att du deployar uppdaterad kod kommer tabellen skapas automatiskt.

## Efter migration

När tabellen är skapad:
1. Verifiera i Supabase att tabellen finns
2. Testa att skapa en delningslänk i appen
3. Kontrollera att länken fungerar på `/s/{token}`

## Verifiering

Kör detta i Supabase SQL Editor för att verifiera:

```sql
-- Kontrollera att tabellen finns
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name = 'SharedScheduleLinks';

-- Kontrollera att kolumnerna finns
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'SharedScheduleLinks'
ORDER BY ordinal_position;

-- Kontrollera att index finns
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'SharedScheduleLinks';
```
