#!/bin/bash

# Stop any existing server processes
pkill -9 -f "dotnet.*SchedulerMVP" 2>/dev/null
sleep 1
lsof -ti:8080 2>/dev/null | xargs kill -9 2>/dev/null

# Navigate to project directory
cd "$(dirname "$0")/SchedulerMVP"

# Start server in background with nohup
nohup dotnet run --urls "http://localhost:8080" > /tmp/scheduler_output.log 2>&1 &
SERVER_PID=$!

# Save PID for later reference
echo $SERVER_PID > /tmp/scheduler_pid.txt

# Wait a bit for server to start
sleep 8

# Check if server started successfully
if kill -0 $SERVER_PID 2>/dev/null; then
    if lsof -ti:8080 > /dev/null 2>&1; then
        echo "✅ Server started successfully on port 8080"
        echo "PID: $SERVER_PID"
        echo "Logs: /tmp/scheduler_output.log"
        echo ""
        echo "To stop the server: kill $SERVER_PID"
        echo "Or run: ./stop-server.sh"
    else
        echo "⚠️ Server process started but port not active yet"
        echo "Check logs: tail -f /tmp/scheduler_output.log"
    fi
else
    echo "❌ Server failed to start"
    echo "Check logs: tail -50 /tmp/scheduler_output.log"
    exit 1
fi
