# SchedulerMVP

MVP för **veckoschemaläggning** i SportAdmin – byggt i **Blazor Server (.NET 9)** med **EF Core**.  
Fokus: veckomall (Mån–Sön), drag-&amp;-drop, konfliktlogik via **leaf-coverage**, ghost-visning och mallhantering.

---

## Förutsättningar

- .NET 9 SDK
- (Valfritt) Supabase Postgres-konto om du vill köra mot Postgres i stället för lokal SQLite.

---

## Kom igång (snabbt – SQLite)

```bash
dotnet restore
dotnet run
```

Appen migrerar databasen automatiskt (skapar `app.db`) och seedar:
- **Plats:** Malmö IP
- **Ytor:** Helplan → Halv A/B → Kvarts A1/A2/B1/B2 (coverage satt mot leafs A1, A2, B1, B2)
- **Grupper:** F2012, F2011, Dam A, Herr A (med färger)
- **Mall:** “Veckoschema HT2025 (exempel)” med några bokningar för att demonstrera ghost-logik

Öppna appen och:
1) Välj **Malmö IP**  
2) Dra en grupp från högerkolumnen in i veckogriden  
3) Spara i modalen (konflikter valideras i realtid)

---

## Köra mot Supabase (Postgres)

1. Skapa en databas i Supabase.  
2. Lägg en `.env` i projektroten med **exakt** dessa nycklar:

```
DB_PROVIDER=postgres
POSTGRES_CONNECTION_STRING=Host=YOUR_HOST;Port=5432;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PW;Ssl Mode=Require;Trust Server Certificate=true
```

3. Starta appen:

```bash
dotnet run
```

> I dev körs `Database.Migrate()` + seed automatiskt.  
> För att gå tillbaka till SQLite, ändra `DB_PROVIDER=sqlite` (eller ta bort `.env`).

---

## Hälsa &amp; felsökning

- **Health-check:** `GET /health` visar aktiv provider och 200 OK om DB nås.  
- **Rensa lokal DB:** stoppa appen och ta bort `app.db`, kör sedan `dotnet run` igen.  
- **Migrationer:** ligger under `Data/Migrations/` och appliceras automatiskt i dev.

---

## Routing & sidor

- Alla routade sidor ligger i `SchedulerMVP/Components/Pages/`.
- `SchedulerMVP/Pages/` används endast för `_Host.cshtml` (inga sidor här).
- Varje sida ska ha unik `@page "/..."` och en `PageTitle`.
- Standardlayout är `Layout.MainLayout`. Sidor utan TopBar använder `@layout Layout.LoginLayout`.
- Rutt-namn: gemener, inga mellanslag/åäö. Ex: `/booking-template`, `/settings`.

Skapa ny sida (minimimall):

```razor
@page "/din-rutt"
<PageTitle>Din titel</PageTitle>

<h1>Din titel</h1>
```

Kontrollera dubbletter (kräver `ripgrep`/`rg`):

```bash
make routes
```

Kommandot listar alla rutter och avbryter med fel om dubbletter hittas.

---

## Viktiga begrepp

- **Leafs:** minsta bokningsbara enheter (ex. A1, A2, B1, B2 eller simbanor 1..8/halvor).  
- **Area &amp; AreaLeaf:** en ytas **coverage** = vilka leafs den blockerar.  
- **Konflikt:** `tid överlappar` **och** `coverage(A) ∩ coverage(B) ≠ ∅`.  
- **Ghost:** visas på ytor som påverkas av en bokning men inte är den exakta ytan.

---

## Om Cursor tappar chatten

All specifikation finns i **`PromptSpec.md`** i repo-roten.  
Kopiera innehållet därifrån och klistra in i en ny Cursor-chat för att fortsätta bygga vidare utan att tappa riktningen.

---

## Licens

Internal MVP – ej för extern distribution.
