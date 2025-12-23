#!/bin/bash

# Script to sync current branch with main before starting work
# Usage: ./sync-with-main.sh

set -e

echo "🔄 Syncing with main..."

# Get current branch name
CURRENT_BRANCH=$(git branch --show-current)
echo "📍 Current branch: $CURRENT_BRANCH"

# Fetch latest from origin
echo "📥 Fetching latest from origin..."
git fetch origin

# Check if we're behind main
BEHIND=$(git rev-list --count HEAD..origin/main 2>/dev/null || echo "0")
AHEAD=$(git rev-list --count origin/main..HEAD 2>/dev/null || echo "0")

if [ "$BEHIND" -gt "0" ]; then
    echo "⚠️  Branch is $BEHIND commit(s) behind main"
    echo "🔄 Merging main into $CURRENT_BRANCH..."
    git merge origin/main
    echo "✅ Synced with main"
    
    # Check if we need to push
    if [ "$AHEAD" -gt "0" ]; then
        echo "📤 Pushing synced branch..."
        git push origin "$CURRENT_BRANCH"
        echo "✅ Pushed to origin"
    fi
else
    echo "✅ Already up to date with main"
fi

# Show final status
echo ""
echo "📊 Final status:"
echo "   Ahead of main:  $AHEAD commit(s)"
echo "   Behind main:    $BEHIND commit(s)"
echo ""
echo "✅ Ready to work!"

