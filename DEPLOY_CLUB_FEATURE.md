# Säker Deployment-plan för Föreningsfunktionen

## Problem från förra försöket
1. Migrations kördes automatiskt när appen startade
2. Om migrations misslyckade kraschade appen
3. Svårt att felsöka i produktion

## Säker strategi: Migrations FÖRST, sedan kod

### Steg 1: Verifiera migrations lokalt (valfritt men rekommenderat)
```bash
# Om du har lokal Supabase-anslutning:
./run_migrations.sh
```

### Steg 2: Deploya migrations MANUELLT först
**KRITISKT**: Kör migrations INNAN vi deployar koden. Om migrations misslyckas, kraschar inte appen eftersom koden inte är deployad.

```bash
# Kör migrations mot produktion manuellt:
cd SchedulerMVP
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="<SUPABASE_CONNECTION_STRING>" \
dotnet ef database update --context ApplicationDbContext

ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="<SUPABASE_CONNECTION_STRING>" \
dotnet ef database update --context AppDbContext
```

**ELLER** använd run_migrations.sh (men uppdatera connection string först)

### Steg 3: Verifiera att migrations kördes korrekt
Kontrollera i Supabase SQL Editor:
- `Clubs` tabellen finns
- `AspNetUsers.ClubId` kolumn finns
- `Places.ClubId`, `Groups.ClubId`, `ScheduleTemplates.ClubId`, `GroupTypes.ClubId` finns

### Steg 4: Deploya koden
När migrations är klara, mergea branchen till main:
```bash
git checkout main
git merge feature/clubs-foreningar
git push origin main
```

GitHub Actions kommer automatiskt deploya koden. Eftersom migrations redan är körda, kommer appen bara behöva starta normalt.

## Varför detta är säkrare

1. **Migrations körs FÖRST** - Om något går fel, kraschar inte appen
2. **Separata steg** - Lättare att felsöka om något går fel
3. **Verifierbar** - Vi kan kontrollera att migrations fungerade innan vi deployar kod
4. **Rollback** - Om migrations misslyckas, kan vi fixa dem utan att appen är nere

## Alternativ: Om manuell migration inte fungerar

Om `dotnet ef database update` inte fungerar, kan vi:
1. Kopiera migrations-SQL från migrations-filerna
2. Köra SQL direkt i Supabase SQL Editor
3. Sedan deploya koden

