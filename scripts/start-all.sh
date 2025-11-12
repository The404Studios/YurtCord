#!/bin/bash

# YurtCord Complete Startup Script
# Starts both backend and frontend in development mode

set -e

echo "🚀 Starting YurtCord Complete Stack"
echo "===================================="

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${YELLOW}⚠️  Docker is not running. Starting Docker...${NC}"
    # Try to start Docker (works on Linux with systemd)
    if command -v systemctl &> /dev/null; then
        sudo systemctl start docker
    fi
fi

# Start infrastructure (PostgreSQL, Redis, MinIO)
echo -e "${BLUE}🐳 Starting infrastructure services...${NC}"
docker-compose up -d postgres redis minio

# Wait for services to be ready
echo -e "${BLUE}⏳ Waiting for services to be ready...${NC}"
sleep 5

# Start backend
echo -e "${BLUE}🔧 Starting backend API...${NC}"
cd Backend/YurtCord.API
dotnet run &
BACKEND_PID=$!
cd ../..

# Wait for backend to start
echo -e "${BLUE}⏳ Waiting for backend to start...${NC}"
sleep 10

# Start frontend
echo -e "${BLUE}🎨 Starting frontend...${NC}"
cd Frontend
npm run dev &
FRONTEND_PID=$!
cd ..

echo ""
echo -e "${GREEN}✅ YurtCord is starting up!${NC}"
echo ""
echo -e "${BLUE}Services:${NC}"
echo -e "  Frontend:    ${GREEN}http://localhost:3000${NC}"
echo -e "  Backend API: ${GREEN}http://localhost:5000${NC}"
echo -e "  Swagger UI:  ${GREEN}http://localhost:5000/swagger${NC}"
echo -e "  PostgreSQL:  ${GREEN}localhost:5432${NC}"
echo -e "  Redis:       ${GREEN}localhost:6379${NC}"
echo -e "  MinIO:       ${GREEN}http://localhost:9001${NC}"
echo ""
echo -e "${YELLOW}Press Ctrl+C to stop all services${NC}"

# Cleanup function
cleanup() {
    echo ""
    echo -e "${BLUE}🛑 Stopping services...${NC}"
    kill $BACKEND_PID 2>/dev/null || true
    kill $FRONTEND_PID 2>/dev/null || true
    docker-compose down
    echo -e "${GREEN}✅ All services stopped${NC}"
    exit 0
}

trap cleanup SIGINT SIGTERM

# Keep script running
wait
