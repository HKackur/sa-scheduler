# Azure Secret Setup

Azure credentials har skapats och finns i `azure-credentials.json`.

## Lägg till som GitHub Secret

**Metod 1: Via GitHub Web Interface (enklast)**

1. Öppna: https://github.com/HKackur/sa-scheduler/settings/secrets/actions
2. Klicka "New repository secret"
3. **Name:** `AZURE_CREDENTIALS`
4. **Value:** Kopiera hela innehållet från `azure-credentials.json`
5. Klicka "Add secret"

**Metod 2: Via Script (kräver GitHub Personal Access Token)**

```bash
export GITHUB_TOKEN=your_github_personal_access_token
./add-azure-secret.sh
```

För att skapa GitHub Personal Access Token:
1. Gå till: https://github.com/settings/tokens
2. "Generate new token (classic)"
3. Välj scope: `repo` (full control)
4. Kopiera token och använd i scriptet ovan

## Efter att secret är tillagt

Nästa push till `main` kommer automatiskt deploya till Azure App Service.

Eller kör workflowen manuellt:
1. Gå till: https://github.com/HKackur/sa-scheduler/actions
2. Välj "Deploy to Azure App Service"
3. Klicka "Run workflow"

