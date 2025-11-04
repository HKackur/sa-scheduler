# Azure App Service Deployment Guide

Denna guide visar hur du deployar SchedulerMVP till Azure App Service.

## Förutsättningar

1. **Azure-konto** (gratis tier finns för test)
2. **Azure CLI** installerad (`az`)
3. **.NET 9 SDK** installerad
4. **Supabase databas** (eller annan PostgreSQL)

## Steg 1: Förberedelser

### 1.1 Logga in på Azure
```bash
az login
```

### 1.2 Skapa en resursgrupp
```bash
az group create --name scheduler-mvp-rg --location westeurope
```

## Steg 2: Skapa App Service Plan

Välj en plan baserat på dina behov:

**Basic B1 (rekommenderat för start):**
```bash
az appservice plan create \
  --name scheduler-mvp-plan \
  --resource-group scheduler-mvp-rg \
  --sku B1 \
  --is-linux
```

**Kostnad:** ~$13/månad

**Alternativ - Free tier (endast för test):**
```bash
az appservice plan create \
  --name scheduler-mvp-plan \
  --resource-group scheduler-mvp-rg \
  --sku FREE \
  --is-linux
```

## Steg 3: Skapa Web App

```bash
az webapp create \
  --name scheduler-mvp-app \
  --resource-group scheduler-mvp-rg \
  --plan scheduler-mvp-plan \
  --runtime "DOTNET|9.0"
```

## Steg 4: Konfigurera App Settings

### 4.1 Databas connection string
```bash
az webapp config connection-string set \
  --name scheduler-mvp-app \
  --resource-group scheduler-mvp-rg \
  --connection-string-type PostgreSQL \
  --settings DefaultConnection="Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;Trust Server Certificate=true"
```

### 4.2 Environment variabler
```bash
az webapp config appsettings set \
  --name scheduler-mvp-app \
  --resource-group scheduler-mvp-rg \
  --settings ASPNETCORE_ENVIRONMENT=Production
```

## Steg 5: Aktivera ARR Affinity (Sticky Sessions)

**KRITISKT för Blazor Server!** Detta säkerställer att SignalR-anslutningar håller sig till samma server:

```bash
az webapp config set \
  --name scheduler-mvp-app \
  --resource-group scheduler-mvp-rg \
  --generic-configurations '{"alwaysOn": true, "arrAffinityEnabled": true}'
```

Eller via Azure Portal:
1. Gå till din Web App
2. Settings → Configuration → General settings
3. Aktivera "ARR Affinity" (Always On bör också vara aktiverat)

## Steg 6: Deploy applikationen

### Alternativ A: Deploy från lokal build
```bash
# Bygg applikationen
cd SchedulerMVP
dotnet publish -c Release -o ./publish

# Deploy till Azure
az webapp deploy \
  --name scheduler-mvp-app \
  --resource-group scheduler-mvp-rg \
  --type zip \
  --src-path ./publish.zip
```

### Alternativ B: Deploy via GitHub Actions (rekommenderat)

Skapa `.github/workflows/azure-deploy.yml`:

```yaml
name: Deploy to Azure App Service

on:
  push:
    branches:
      - main

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Restore dependencies
        run: dotnet restore SchedulerMVP/SchedulerMVP.csproj
      
      - name: Build
        run: dotnet build SchedulerMVP/SchedulerMVP.csproj --configuration Release --no-restore
      
      - name: Publish
        run: dotnet publish SchedulerMVP/SchedulerMVP.csproj --configuration Release --output ./publish
      
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: scheduler-mvp-app
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

För att få publish profile:
1. Azure Portal → din Web App → Get publish profile
2. Kopiera innehållet
3. Lägg till som GitHub Secret: `AZURE_WEBAPP_PUBLISH_PROFILE`

## Steg 7: Verifiera deployment

1. Öppna din app: `https://scheduler-mvp-app.azurewebsites.net`
2. Testa login
3. Verifiera att SignalR fungerar (Blazor Server connections)

## Steg 8: Konfigurera Custom Domain (valfritt)

```bash
az webapp config hostname add \
  --webapp-name scheduler-mvp-app \
  --resource-group scheduler-mvp-rg \
  --hostname din-domän.se
```

## Viktiga inställningar för Blazor Server

✅ **ARR Affinity** - Aktiverad (sticky sessions)
✅ **Always On** - Aktiverad (förhindrar kallstart)
✅ **WebSockets** - Aktiverade automatiskt i Azure App Service
✅ **DataProtection** - Konfigurerad i koden (använder temp directory)

## Troubleshooting

### SignalR-anslutningar fungerar inte
- Kontrollera att ARR Affinity är aktiverad
- Verifiera att WebSockets är aktiverade (automatiskt i Azure)

### Authentication cookies fungerar inte efter restart
- DataProtection är konfigurerad att använda temp directory
- För production, överväg Azure Blob Storage eller Azure Key Vault

### Appen startar inte
- Kontrollera logs: `az webapp log tail --name scheduler-mvp-app --resource-group scheduler-mvp-rg`
- Verifiera connection string är korrekt
- Kontrollera att .NET 9.0 runtime är vald

## Kostnad

- **Basic B1 plan:** ~$13/månad (1 CPU, 1.75GB RAM)
- **Free tier:** $0/månad (begränsad till 1 instance, ingen ARR Affinity)
- **Supabase databas:** Separat kostnad (eller använd gratis tier)

## Nästa steg

- Konfigurera CI/CD pipeline
- Sätt upp monitoring och alerts
- Konfigurera backup-strategi för databas

