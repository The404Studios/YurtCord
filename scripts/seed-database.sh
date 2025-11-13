#!/bin/bash

# YurtCord Database Seeding Script
# Seeds the database with example data for development

set -e

echo "🌱 YurtCord Database Seeder"
echo "============================"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Check if we're in the right directory
if [ ! -f "Backend/YurtCord.API/YurtCord.API.csproj" ]; then
    echo -e "${RED}❌ Error: Must be run from the YurtCord root directory${NC}"
    exit 1
fi

echo -e "${YELLOW}⚠️  Warning: This will seed the database with example data.${NC}"
echo -e "${YELLOW}   If the database already has users, seeding will be skipped.${NC}"
echo ""
read -p "Do you want to continue? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cancelled."
    exit 0
fi

echo ""
echo -e "${BLUE}🚀 Starting database seeding...${NC}"

# Make sure infrastructure is running
echo -e "${BLUE}📦 Checking infrastructure services...${NC}"
if ! docker ps | grep -q postgres; then
    echo -e "${YELLOW}⚠️  PostgreSQL is not running. Starting it now...${NC}"
    docker-compose up -d postgres
    echo "Waiting for PostgreSQL to be ready..."
    sleep 5
fi

# Set environment variable and run API briefly
echo -e "${BLUE}🌱 Seeding database...${NC}"
cd Backend/YurtCord.API

# Export environment variable
export SEED_DATABASE=true

# Run the API (it will seed and then we'll stop it)
echo -e "${YELLOW}Starting API with seeding enabled...${NC}"
timeout 30s dotnet run || true

cd ../..

echo ""
echo -e "${GREEN}✅ Database seeding completed!${NC}"
echo ""
echo -e "${BLUE}📝 Example accounts created:${NC}"
echo "  Email: admin@yurtcord.com     | Password: Admin123!"
echo "  Email: alice@example.com      | Password: Password123!"
echo "  Email: bob@example.com        | Password: Password123!"
echo "  Email: charlie@example.com    | Password: Password123!"
echo "  Email: diana@example.com      | Password: Password123!"
echo ""
echo -e "${BLUE}📊 Example data:${NC}"
echo "  • 5 users with profiles and presence"
echo "  • 3 guilds (servers)"
echo "  • Channels (text, voice, categories)"
echo "  • Roles with permissions"
echo "  • Example messages"
echo "  • Message reactions"
echo ""
echo -e "${GREEN}🎉 You can now start the application and login!${NC}"
