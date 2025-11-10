#!/bin/bash

# Azure App Service Stability Configuration Script
# Detta skript konfigurerar alla inställningar för stabil drift

set -e

# Konfiguration - UPPDATERA DETTA
APP_NAME="scheduler-mvp-app-1762239380"
RESOURCE_GROUP="scheduler-mvp-rg"  # Uppdatera om annat namn

echo "🚀 Konfigurerar Azure App Service för stabil drift..."
echo "App: $APP_NAME"
echo "Resource Group: $RESOURCE_GROUP"
echo ""

# 1. Aktivera Always On (förhindrar kallstart)
echo "✅ Aktiverar Always On..."
az webapp config set \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --always-on true

# 2. Aktivera Application Logging (Filesystem)
echo "✅ Aktiverar Application Logging..."
az webapp log config \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --application-logging filesystem \
  --level information

# 3. Aktivera HTTP Logging
echo "✅ Aktiverar HTTP Logging..."
az webapp log config \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --web-server-logging filesystem

# 4. Aktivera ARR Affinity (kritisk för Blazor Server)
echo "✅ Aktiverar ARR Affinity (sticky sessions)..."
az webapp update \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --set clientAffinityEnabled=true

# 5. Aktivera WebSockets (för SignalR)
echo "✅ Aktiverar WebSockets..."
az webapp config set \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --web-sockets-enabled true

# 6. Verifiera konfiguration
echo ""
echo "📋 Verifierar konfiguration..."
echo ""
echo "Always On:"
az webapp config show \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "alwaysOn" \
  --output tsv

echo ""
echo "Logging konfiguration:"
az webapp log show \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP"

echo ""
echo "✅ Konfiguration klar!"
echo ""
echo "📝 Nästa steg:"
echo "1. Testa health endpoint: https://$APP_NAME.azurewebsites.net/health"
echo "2. Öppna log stream: az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP"
echo "3. Verifiera att appen startar snabbt efter deploy"

