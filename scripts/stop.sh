#!/bin/bash

# YurtCord Stop Script
# This script stops both Backend and Frontend services

echo "🛑 Stopping YurtCord..."
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

stopped=false

# Stop processes using PID files
if [ -f "/tmp/yurtcord-backend.pid" ]; then
    BACKEND_PID=$(cat /tmp/yurtcord-backend.pid)
    if kill -0 $BACKEND_PID 2>/dev/null; then
        kill $BACKEND_PID 2>/dev/null
        echo -e "${GREEN}✓ Backend stopped${NC}"
        stopped=true
    fi
    rm -f /tmp/yurtcord-backend.pid
fi

if [ -f "/tmp/yurtcord-frontend.pid" ]; then
    FRONTEND_PID=$(cat /tmp/yurtcord-frontend.pid)
    if kill -0 $FRONTEND_PID 2>/dev/null; then
        kill $FRONTEND_PID 2>/dev/null
        echo -e "${GREEN}✓ Frontend stopped${NC}"
        stopped=true
    fi
    rm -f /tmp/yurtcord-frontend.pid
fi

# Also try to kill processes on ports 5000 and 5173
echo ""
echo -e "${BLUE}🔍 Checking for processes on ports 5000 and 5173...${NC}"

# Kill process on port 5000 (Backend)
BACKEND_PID=$(lsof -ti:5000 2>/dev/null)
if [ ! -z "$BACKEND_PID" ]; then
    kill $BACKEND_PID 2>/dev/null
    echo -e "${GREEN}✓ Killed process on port 5000${NC}"
    stopped=true
fi

# Kill process on port 5173 (Frontend)
FRONTEND_PID=$(lsof -ti:5173 2>/dev/null)
if [ ! -z "$FRONTEND_PID" ]; then
    kill $FRONTEND_PID 2>/dev/null
    echo -e "${GREEN}✓ Killed process on port 5173${NC}"
    stopped=true
fi

# Clean up log files
rm -f /tmp/yurtcord-backend.log

echo ""
if [ "$stopped" = true ]; then
    echo -e "${GREEN}✨ YurtCord stopped successfully!${NC}"
else
    echo -e "${CYAN}ℹ️  No running YurtCord processes found${NC}"
fi
