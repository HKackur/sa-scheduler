#!/bin/bash

# Azure App Service Restart Script
# Kör detta skript när din prenumeration är helt reaktiverad

set -e

# Konfiguration
APP_NAME="scheduler-mvp-app-1762239380"
RESOURCE_GROUP="scheduler-mvp-rg"
PLAN_NAME="scheduler-mvp-plan"

echo "🔄 Startar om Azure App Service..."
echo "App: $APP_NAME"
echo "Resource Group: $RESOURCE_GROUP"
echo ""

# 1. Kontrollera prenumerationsstatus
echo "📋 Kontrollerar prenumerationsstatus..."
SUBSCRIPTION_STATE=$(az account show --query "state" --output tsv)
echo "Prenumerationsstatus: $SUBSCRIPTION_STATE"
echo ""

if [ "$SUBSCRIPTION_STATE" != "Enabled" ]; then
    echo "❌ Prenumerationen är inte aktiverad. Vänligen aktivera den i Azure Portal först."
    exit 1
fi

# 2. Skala upp App Service Plan till 1 worker (om den är nere på 0)
echo "📈 Skalar upp App Service Plan..."
CURRENT_WORKERS=$(az appservice plan show --name "$PLAN_NAME" --resource-group "$RESOURCE_GROUP" --query "sku.capacity" --output tsv 2>/dev/null || echo "0")

if [ "$CURRENT_WORKERS" == "0" ] || [ -z "$CURRENT_WORKERS" ]; then
    echo "   Skalar upp från 0 till 1 worker..."
    az appservice plan update \
        --name "$PLAN_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --number-of-workers 1
    echo "   ✅ Plan skalad upp"
else
    echo "   Plan har redan $CURRENT_WORKERS worker(s)"
fi
echo ""

# 3. Starta webbappen
echo "🚀 Startar webbapp..."
az webapp start \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP"
echo "   ✅ Webbapp startad"
echo ""

# 4. Aktivera Always On (förhindrar att appen stängs av)
echo "⚙️  Aktiverar Always On..."
az webapp config set \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --always-on true
echo "   ✅ Always On aktiverad"
echo ""

# 5. Verifiera status
echo "📊 Verifierar status..."
sleep 5
APP_STATE=$(az webapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --query "state" --output tsv)
echo "   App status: $APP_STATE"
echo ""

if [ "$APP_STATE" == "Running" ]; then
    echo "✅ Appen är nu igång!"
    echo ""
    echo "🌐 Öppna appen: https://sportadminschema.se"
    echo "   eller: https://$APP_NAME.azurewebsites.net"
    echo ""
    echo "📝 För att se loggar:"
    echo "   az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP"
else
    echo "⚠️  Appen är inte i 'Running' status ännu. Status: $APP_STATE"
    echo "   Vänta några minuter och kör skriptet igen, eller kontrollera i Azure Portal."
fi











