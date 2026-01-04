#!/bin/bash
set -e

echo "🔧 Creating Modals tables in Supabase..."

# Install psql if needed (macOS)
if ! command -v psql &> /dev/null; then
    echo "Installing PostgreSQL client..."
    brew install libpq
    export PATH="/opt/homebrew/opt/libpq/bin:$PATH"
fi

# Run SQL script
PGPASSWORD="bunch-hiccups-misery-extreme" \
psql -h db.anebyqfrzsuqwrbncwxt.supabase.co \
     -p 5432 \
     -U postgres \
     -d postgres \
     -f create_modals_tables.sql

echo "✅ Modals tables created!"

