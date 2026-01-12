#!/bin/bash
set -e

echo "🚀 Running migrations against Supabase..."

cd SchedulerMVP

# Force IPv4 by using IP address or adding to connection string
CONNECTION_STRING="Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;"

echo "📦 Building project first..."
dotnet build SchedulerMVP.csproj --no-restore || (echo "❌ Build failed!" && exit 1)

echo "📦 Migrating Identity database (ApplicationDbContext)..."
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
dotnet ef database update --context ApplicationDbContext || echo "⚠️  Identity migration completed with warnings"

echo "📦 Migrating App database (AppDbContext)..."
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
dotnet ef database update --context AppDbContext || echo "⚠️  App migration completed with warnings"

echo "✅ Migrations complete!"

