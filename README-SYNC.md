# Synkning med Main

## Problem
Brancher hamnar ofta efter `main` när PR:er mergas, vilket orsakar konflikter och förvirring.

## Lösning
Använd `sync-with-main.sh` scriptet **innan** du börjar arbeta:

```bash
./sync-with-main.sh
```

## Vad scriptet gör
1. ✅ Hämtar senaste från origin
2. ✅ Kontrollerar om branchen är efter main
3. ✅ Mergar main automatiskt om det behövs
4. ✅ Pushar om branchen har commits före main
5. ✅ Visar status (ahead/behind)

## När ska du köra det?
- **Innan** du börjar arbeta på nya ändringar
- **Efter** att någon merge:at en PR till main
- **Innan** du skapar en ny PR
- **När** GitHub visar "X commits behind main"

## Exempel
```bash
$ ./sync-with-main.sh
🔄 Syncing with main...
📍 Current branch: feature/performance-optimization
📥 Fetching latest from origin...
⚠️  Branch is 1 commit(s) behind main
🔄 Merging main into feature/performance-optimization...
✅ Synced with main
📤 Pushing synced branch...
✅ Pushed to origin

📊 Final status:
   Ahead of main:  2 commit(s)
   Behind main:    0 commit(s)

✅ Ready to work!
```


