# Hur man kör migrations manuellt

## Viktigt: Scriptet körs i TERMINALEN, inte i Supabase SQL Editor!

Supabase SQL Editor är för SQL-queries, inte för bash-script.

## Steg 1: Öppna terminal

Öppna en terminal i projektroten (`/Users/henrikkackur/SchedulerMVP`)

## Steg 2: Se till att du är på rätt branch

```bash
git checkout feature/clubs-foreningar
```

## Steg 3: Kör migrations-scriptet

```bash
./run_migrations.sh
```

Detta kommer:
1. Bygga projektet
2. Köra migrations för ApplicationDbContext (lägger till ClubId på AspNetUsers)
3. Köra migrations för AppDbContext (skapar Clubs tabell och ClubId kolumner)

## Alternativ: Köra migrations manuellt steg för steg

Om scriptet inte fungerar, kör dessa kommandon manuellt:

```bash
cd SchedulerMVP

# Connection string (redan i scriptet)
CONNECTION_STRING="Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;"

# Migrera Identity database
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
dotnet ef database update --context ApplicationDbContext

# Migrera App database  
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
dotnet ef database update --context AppDbContext
```

## Efter migrations

När migrations är klara, verifiera i Supabase SQL Editor (här använder du SQL!):

```sql
-- Kontrollera att Clubs tabellen finns
SELECT * FROM "Clubs" LIMIT 1;

-- Kontrollera att ClubId kolumn finns på AspNetUsers
SELECT "ClubId" FROM "AspNetUsers" LIMIT 1;

-- Kontrollera att ClubId kolumner finns på andra tabeller
SELECT "ClubId" FROM "Places" LIMIT 1;
SELECT "ClubId" FROM "Groups" LIMIT 1;
SELECT "ClubId" FROM "ScheduleTemplates" LIMIT 1;
SELECT "ClubId" FROM "GroupTypes" LIMIT 1;
```

Om alla queries fungerar (ingen "column does not exist" error), är migrations klara!

## Nästa steg

När migrations är klara, mergea feature-branch till main och deploya koden:

```bash
git checkout main
git merge feature/clubs-foreningar
git push origin main
```

