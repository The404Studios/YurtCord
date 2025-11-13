#!/bin/bash

# YurtCord Health Check Script
# Verifies that all components are working correctly

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🏥 YurtCord Health Check"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

SUCCESS=0
WARNINGS=0
FAILURES=0

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to print status
print_status() {
    local status=$1
    local message=$2

    if [ "$status" = "success" ]; then
        echo -e "${GREEN}✓${NC} $message"
        SUCCESS=$((SUCCESS + 1))
    elif [ "$status" = "warning" ]; then
        echo -e "${YELLOW}⚠${NC} $message"
        WARNINGS=$((WARNINGS + 1))
    else
        echo -e "${RED}✗${NC} $message"
        FAILURES=$((FAILURES + 1))
    fi
}

# Check prerequisites
echo -e "${BLUE}📋 Checking Prerequisites...${NC}"
echo ""

if command_exists dotnet; then
    DOTNET_VERSION=$(dotnet --version)
    print_status "success" ".NET SDK installed: $DOTNET_VERSION"
else
    print_status "failure" ".NET SDK not found (required for backend)"
fi

if command_exists node; then
    NODE_VERSION=$(node --version)
    print_status "success" "Node.js installed: $NODE_VERSION"
else
    print_status "failure" "Node.js not found (required for frontend)"
fi

if command_exists npm; then
    NPM_VERSION=$(npm --version)
    print_status "success" "npm installed: $NPM_VERSION"
else
    print_status "failure" "npm not found (required for frontend)"
fi

echo ""

# Check project structure
echo -e "${BLUE}📁 Checking Project Structure...${NC}"
echo ""

if [ -d "Backend/YurtCord.API" ]; then
    print_status "success" "Backend directory exists"
else
    print_status "failure" "Backend directory not found"
fi

if [ -f "Backend/YurtCord.API/Program.cs" ]; then
    print_status "success" "Backend entry point exists"
else
    print_status "failure" "Backend Program.cs not found"
fi

if [ -d "Frontend" ]; then
    print_status "success" "Frontend directory exists"
else
    print_status "failure" "Frontend directory not found"
fi

if [ -f "Frontend/package.json" ]; then
    print_status "success" "Frontend package.json exists"
else
    print_status "failure" "Frontend package.json not found"
fi

if [ -d "Frontend/node_modules" ]; then
    print_status "success" "Frontend dependencies installed"
else
    print_status "warning" "Frontend dependencies not installed (run: cd Frontend && npm install)"
fi

echo ""

# Check configuration files
echo -e "${BLUE}⚙️  Checking Configuration...${NC}"
echo ""

if [ -f "Backend/YurtCord.API/appsettings.json" ]; then
    print_status "success" "Backend configuration exists"
else
    print_status "failure" "Backend appsettings.json not found"
fi

if [ -f "Frontend/.env" ]; then
    print_status "success" "Frontend .env file exists"
else
    if [ -f "Frontend/.env.example" ]; then
        print_status "warning" "Frontend .env not found (copy from .env.example)"
    else
        print_status "warning" "Frontend .env not found"
    fi
fi

echo ""

# Check if services are running
echo -e "${BLUE}🔌 Checking Running Services...${NC}"
echo ""

# Check Backend API
if curl -s -f http://localhost:5000/health >/dev/null 2>&1; then
    print_status "success" "Backend API is running (http://localhost:5000)"
else
    print_status "warning" "Backend API not running (start with: cd Backend/YurtCord.API && dotnet run)"
fi

# Check Frontend
if curl -s -f http://localhost:5173 >/dev/null 2>&1; then
    print_status "success" "Frontend is running (http://localhost:5173)"
else
    print_status "warning" "Frontend not running (start with: cd Frontend && npm run dev)"
fi

# Check Swagger
if curl -s -f http://localhost:5000/swagger/index.html >/dev/null 2>&1; then
    print_status "success" "Swagger UI accessible (http://localhost:5000/swagger)"
else
    if curl -s -f http://localhost:5000/health >/dev/null 2>&1; then
        print_status "warning" "Swagger might not be enabled"
    fi
fi

echo ""

# Check database
echo -e "${BLUE}💾 Checking Database...${NC}"
echo ""

if [ -f "Data/yurtcord.db" ]; then
    DB_SIZE=$(du -h "Data/yurtcord.db" | cut -f1)
    print_status "success" "SQLite database exists (${DB_SIZE})"
else
    print_status "warning" "SQLite database not found (will be created on first run)"
fi

echo ""

# Check startup scripts
echo -e "${BLUE}🚀 Checking Startup Scripts...${NC}"
echo ""

if [ -f "scripts/start.sh" ]; then
    if [ -x "scripts/start.sh" ]; then
        print_status "success" "start.sh exists and is executable"
    else
        print_status "warning" "start.sh exists but not executable (run: chmod +x scripts/start.sh)"
    fi
else
    print_status "failure" "start.sh not found"
fi

if [ -f "scripts/stop.sh" ]; then
    if [ -x "scripts/stop.sh" ]; then
        print_status "success" "stop.sh exists and is executable"
    else
        print_status "warning" "stop.sh exists but not executable (run: chmod +x scripts/stop.sh)"
    fi
else
    print_status "failure" "stop.sh not found"
fi

echo ""

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo -e "${BLUE}📊 Summary${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo -e "${GREEN}✓ Success:${NC} $SUCCESS checks passed"
echo -e "${YELLOW}⚠ Warnings:${NC} $WARNINGS warnings"
echo -e "${RED}✗ Failures:${NC} $FAILURES failures"
echo ""

if [ $FAILURES -gt 0 ]; then
    echo -e "${RED}⚠️  Some critical checks failed. Please fix the failures above.${NC}"
    echo ""
    exit 1
elif [ $WARNINGS -gt 0 ]; then
    echo -e "${YELLOW}⚠️  Some warnings detected. Review the warnings above.${NC}"
    echo ""
    exit 0
else
    echo -e "${GREEN}✅ All checks passed! YurtCord is ready to run.${NC}"
    echo ""
    echo "🚀 Start YurtCord:"
    echo "   ./scripts/start.sh"
    echo ""
    echo "📚 Documentation:"
    echo "   GETTING_STARTED.md"
    echo ""
    exit 0
fi
