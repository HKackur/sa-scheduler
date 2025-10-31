# Testinstruktioner för Multi-Tenant Implementation

## Förutsättningar
- .NET 9 SDK installerat
- SQLite database (app.db) kommer skapas automatiskt
- Alla NuGet packages är installerade

## Steg 1: Starta applikationen

Kör i terminalen från projektets rot:
```bash
cd SchedulerMVP
dotnet run
```

## Steg 2: Vad ska fungera just nu

### Vad FUNGERAR:
1. **Applikationen startar** - appen ska starta utan fel
2. **Databasen migreras automatiskt** - Identity-tabeller och UserId-kolumner skapas
3. **Seeda en admin-användare** (sker inte automatiskt än, måste göras manuellt via terminal)
4. **Login-sidan finns** - gå till `/login` i webbläsaren
5. **Platser och mallar** - ska fungera som vanligt (bara för inloggade användare senare)

### Vad FUNGERAR INTE ÄN:
- Login/logout UI (kommer längre ner i högerpanel senare)
- Admin-användare skapas automatiskt (behöver göras manuellt för test)
- [Authorize] på routes (appen är öppen för alla just nu)
- Admin-panel
- Data-rensning

## Steg 3: Skapa admin-användare manuellt (för test)

Eftersom seed ännu inte har uppdaterats, behöver du skapa en admin-användare manuellt.

### Alternativ 1: Via terminal (enklast för test)
Stoppa appen (Ctrl+C) och kör:
```bash
dotnet run --no-build
```

Sedan skapa ett PowerShell/terminal-script eller använd en SQL-fil.

### Alternativ 2: Vänta tills seed är klar
Seed-koden kommer uppdateras nästa för att automatiskt skapa en admin-användare.

## Steg 4: Testa login (när admin är skapat)

1. Gå till `https://localhost:XXXX/login` (port visas i terminalen)
2. Logga in med admin-credentials
3. Du ska kunna navigera tillbaka till huvudsidan

## Steg 5: Kontrollera databasen

Kolla att Identity-tabellerna skapades:
- Öppna `app.db` med SQLite browser eller liknande
- Du ska se tabeller som:
  - `AspNetUsers`
  - `AspNetRoles`
  - `AspNetUserRoles`
  - etc.

## Steg 6: Testa multi-tenant isolering (när flera användare finns)

1. Skapa en testanvändare (via admin-panel när det är klart)
2. Logga in som testanvändare
3. Skapa en plats/grupp som testanvändare
4. Logga in som admin - du ska kunna se all data
5. Logga in som testanvändare igen - du ska bara se dina egna data

## Known Issues / TODO

- Login/logout UI kommer läggas till längre ner i högerpanel
- Admin-panel kommer skapas för att hantera användare
- Seed kommer uppdateras för att automatiskt skapa admin
- [Authorize] kommer läggas till på routes

## Om något inte fungerar

1. Kontrollera att alla packages är installerade: `dotnet restore`
2. Ta bort databasen (`app.db`) och starta om för fresh start
3. Kolla build errors: `dotnet build`

