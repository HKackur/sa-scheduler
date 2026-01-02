#!/bin/bash
# Script to set Email SMTP configuration in Azure App Service

APP_NAME="scheduler-mvp-app-1762239380"
RESOURCE_GROUP="scheduler-mvp-rg"

echo "📧 Setting Email SMTP configuration in Azure..."
echo ""

# Check if variables are set
if [ -z "$BREVO_EMAIL" ] || [ -z "$BREVO_SMTP_KEY" ]; then
    echo "⚠️  Environment variables not set!"
    echo ""
    echo "Set these environment variables first:"
    echo "  export BREVO_EMAIL='din-brevo-email@example.com'"
    echo "  export BREVO_SMTP_KEY='din-smtp-key'"
    echo ""
    echo "Then run this script again:"
    echo "  ./set-azure-email-config.sh"
    exit 1
fi

echo "Setting Email configuration..."
az webapp config appsettings set \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --settings \
    "Email:SmtpHost=smtp-relay.brevo.com" \
    "Email:SmtpPort=587" \
    "Email:SmtpUser=$BREVO_EMAIL" \
    "Email:SmtpPassword=$BREVO_SMTP_KEY" \
    "Email:FromEmail=$BREVO_EMAIL" \
    "Email:FromName=Sportadmins Schemaläggning" \
  --output none

if [ $? -eq 0 ]; then
    echo "✅ Email SMTP configuration set successfully!"
    echo ""
    echo "The app will restart automatically. Wait ~30 seconds, then test password reset."
else
    echo "❌ Failed to set Email configuration"
    exit 1
fi

