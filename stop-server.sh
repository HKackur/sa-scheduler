#!/bin/bash

echo "Stopping SchedulerMVP server..."

# Kill by PID if file exists
if [ -f /tmp/scheduler_pid.txt ]; then
    PID=$(cat /tmp/scheduler_pid.txt)
    if kill -0 $PID 2>/dev/null; then
        kill $PID
        echo "Stopped process $PID"
    fi
    rm /tmp/scheduler_pid.txt
fi

# Kill any remaining dotnet processes for SchedulerMVP
pkill -9 -f "dotnet.*SchedulerMVP" 2>/dev/null

# Free port 8080
lsof -ti:8080 2>/dev/null | xargs kill -9 2>/dev/null

sleep 1

if lsof -ti:8080 > /dev/null 2>&1; then
    echo "⚠️ Port 8080 still in use"
else
    echo "✅ Server stopped"
fi
