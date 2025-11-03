# Development & Deployment Workflow

## Git Branching Strategy

### För produktionsklar app med användare

**Rekommendation: Git Flow med staging-miljö**

```
main (produktion) 
  └── staging (testmiljö innan produktion)
       └── develop (pågående utveckling)
            └── feature/* (enskilda features)
```

### Workflow

1. **Feature-utveckling:**
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/my-feature
   # Gör ändringar
   git commit -m "FEAT: Add feature X"
   git push origin feature/my-feature
   # Skapa Pull Request till develop
   ```

2. **Testa i staging:**
   ```bash
   git checkout staging
   git merge develop
   git push origin staging
   # Deploy till staging-miljö på Fly.io
   # Testa grundligt
   ```

3. **Produktionsrelease:**
   ```bash
   git checkout main
   git merge staging
   git tag -a v1.0.1 -m "Release version 1.0.1"
   git push origin main --tags
   # Deploy till produktion
   ```

## Fly.io Setup för Staging & Production

### Skapa staging-app
```bash
# Skapa ny app för staging
fly apps create sa-scheduler-staging

# Kopiera fly.toml till fly.staging.toml
cp fly.toml fly.staging.toml
# Ändra app name i fly.staging.toml till "sa-scheduler-staging"
```

### GitHub Actions Workflow

**Option 1: Automatisk deploy per branch**
```yaml
# .github/workflows/deploy.yml
on:
  push:
    branches:
      - main      # Auto-deploy to production
      - staging   # Auto-deploy to staging

jobs:
  deploy:
    if: github.ref == 'refs/heads/main'
    # Deploy to production
    
  deploy-staging:
    if: github.ref == 'refs/heads/staging'
    # Deploy to staging
```

**Option 2: Manuell deploy (rekommenderat för nu)**
```bash
# Deploy staging manuellt när du är redo
fly deploy --config fly.staging.toml

# Deploy produktion när staging är testad
fly deploy
```

## Rekommendation för Ditt Läge

### Fase 1: Nu (Innan användare)
- **main** = produktion (enkel process)
- Skapa **feature branches** för större ändringar:
  ```bash
  git checkout -b feature/booking-fix
  # Gör ändringar, testa lokalt
  git commit -m "FIX: Booking refresh issue"
  git push origin feature/booking-fix
  # Merge till main när klar
  ```

### Fase 2: Med användare (När det blir kritiskt)
- **main** = produktion (stabil)
- **staging** = testmiljö (innan produktion)
- Workflow:
  1. Utveckla i feature branch eller develop
  2. Merge till staging → testa
  3. När testat: merge till main → produktion

## Rollback vid Problem

```bash
# Hitta senaste fungerande commit
git log --oneline

# Återställ till specifik commit
git revert <commit-hash>
# ELLER
git reset --hard <commit-hash>  # ENDAST om ingen pushat ännu

# Deploy igen
fly deploy
```

## Tagging för Versioner

```bash
# Efter testad deploy
git tag -a v1.2.0 -m "Release 1.2.0 - Fixed booking refresh"
git push origin v1.2.0

# Lista versioner
git tag

# Checkout specifik version
git checkout v1.2.0
```

## Praktiskt: Så Här Gör Du Nu

### För små fixes (som idag):
```bash
git checkout main
git pull
# Gör ändringar
git commit -m "FIX: Description"
git push
# Auto-deploy via GitHub Actions
```

### För större features:
```bash
git checkout main
git pull
git checkout -b feature/feature-name
# Utveckla och testa lokalt
git commit -m "FEAT: Feature description"
git push origin feature/feature-name
# När klar: merge till main (via PR eller direkt)
```

### När användare kommer:
1. Skapa staging-miljö på Fly.io
2. Deploy staging först, testa
3. När OK: deploy main (produktion)

