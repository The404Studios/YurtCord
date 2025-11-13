#!/bin/bash

# YurtCord Frontend Setup Script
# This script sets up the complete React frontend with all Discord-like components

set -e

echo "🚀 YurtCord Frontend Setup"
echo "=========================="

# Colors
GREEN='\033[0.32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

cd "$(dirname "$0")/../Frontend"

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo -e "${RED}❌ Node.js is not installed. Please install Node.js 18+ first.${NC}"
    exit 1
fi

echo -e "${BLUE}📦 Installing dependencies...${NC}"
npm install

echo -e "${BLUE}🔧 Creating .env file...${NC}"
if [ ! -f .env ]; then
    cp .env.example .env
    echo -e "${GREEN}✅ Created .env file${NC}"
else
    echo -e "${BLUE}ℹ️  .env file already exists${NC}"
fi

echo -e "${BLUE}🎨 Setting up TypeScript configuration...${NC}"
npm run type-check

echo -e "${GREEN}✅ Frontend setup complete!${NC}"
echo ""
echo -e "${BLUE}To start the development server:${NC}"
echo "  cd Frontend"
echo "  npm run dev"
echo ""
echo -e "${BLUE}To build for production:${NC}"
echo "  cd Frontend"
echo "  npm run build"
echo ""
echo -e "${GREEN}Frontend will be available at: http://localhost:3000${NC}"
