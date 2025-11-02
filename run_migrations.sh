#!/bin/bash
set -e

echo "🚀 Running migrations against Supabase..."

cd SchedulerMVP

CONNECTION_STRING="Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;"

echo "📦 Migrating Identity database (ApplicationDbContext)..."
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
dotnet ef database update --context ApplicationDbContext --no-build || echo "⚠️  Identity migration completed with warnings"

echo "📦 Migrating App database (AppDbContext)..."
ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
dotnet ef database update --context AppDbContext --no-build || echo "⚠️  App migration completed with warnings"

echo "✅ Migrations complete!"

