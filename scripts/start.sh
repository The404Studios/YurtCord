#!/bin/bash

# YurtCord Quick Start Script
# This script starts both Backend and Frontend in embedded mode

set -e

echo "🚀 Starting YurtCord..."
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}⚠️  .NET SDK not found. Please install .NET 8.0 SDK${NC}"
    echo "Download from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo -e "${YELLOW}⚠️  Node.js not found. Please install Node.js 18+${NC}"
    echo "Download from: https://nodejs.org/"
    exit 1
fi

echo -e "${GREEN}✓ .NET SDK found: $(dotnet --version)${NC}"
echo -e "${GREEN}✓ Node.js found: $(node --version)${NC}"
echo ""

# Install Frontend dependencies if needed
if [ ! -d "Frontend/node_modules" ]; then
    echo -e "${BLUE}📦 Installing Frontend dependencies...${NC}"
    cd Frontend
    npm install
    cd ..
    echo -e "${GREEN}✓ Frontend dependencies installed${NC}"
    echo ""
fi

# Create .env file if it doesn't exist
if [ ! -f "Frontend/.env" ]; then
    echo -e "${BLUE}📝 Creating Frontend .env file...${NC}"
    cp Frontend/.env.example Frontend/.env
    echo -e "${GREEN}✓ Frontend .env created${NC}"
    echo ""
fi

echo -e "${BLUE}🔧 Starting Backend API (Embedded Mode)...${NC}"
echo "   Database: SQLite (auto-created)"
echo "   API: http://localhost:5000"
echo "   Swagger: http://localhost:5000/swagger"
echo ""

# Start Backend in background
cd Backend/YurtCord.API
dotnet run > /tmp/yurtcord-backend.log 2>&1 &
BACKEND_PID=$!
cd ../..

echo -e "${GREEN}✓ Backend started (PID: $BACKEND_PID)${NC}"
echo ""

# Wait for backend to be ready
echo -e "${BLUE}⏳ Waiting for Backend to be ready...${NC}"
for i in {1..30}; do
    if curl -s http://localhost:5000/health > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Backend is ready!${NC}"
        echo ""
        break
    fi
    if [ $i -eq 30 ]; then
        echo -e "${YELLOW}⚠️  Backend taking longer than expected. Check logs: tail -f /tmp/yurtcord-backend.log${NC}"
        echo ""
    fi
    sleep 1
done

echo -e "${BLUE}🎨 Starting Frontend...${NC}"
echo "   Frontend: http://localhost:5173"
echo ""

# Start Frontend
cd Frontend
npm run dev &
FRONTEND_PID=$!
cd ..

echo -e "${GREEN}✓ Frontend started (PID: $FRONTEND_PID)${NC}"
echo ""

# Save PIDs for cleanup
echo $BACKEND_PID > /tmp/yurtcord-backend.pid
echo $FRONTEND_PID > /tmp/yurtcord-frontend.pid

echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}✨ YurtCord is running!${NC}"
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo -e "${BLUE}📍 Access Points:${NC}"
echo "   🌐 Frontend:  http://localhost:5173"
echo "   🔧 API:       http://localhost:5000"
echo "   📚 Swagger:   http://localhost:5000/swagger"
echo ""
echo -e "${BLUE}🔐 Test Accounts:${NC}"
echo "   Email: alice@example.com"
echo "   Password: Password123!"
echo ""
echo -e "${BLUE}📊 View Logs:${NC}"
echo "   Backend:  tail -f /tmp/yurtcord-backend.log"
echo "   Frontend: Check the terminal above"
echo ""
echo -e "${YELLOW}💡 To stop YurtCord, run: ./scripts/stop.sh${NC}"
echo ""
echo -e "${GREEN}Press Ctrl+C to stop all services${NC}"

# Wait for Ctrl+C
trap "echo ''; echo '🛑 Stopping YurtCord...'; kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; rm -f /tmp/yurtcord-*.pid; echo '✓ YurtCord stopped'; exit 0" INT

# Keep script running
wait
