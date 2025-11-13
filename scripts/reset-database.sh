#!/bin/bash

# YurtCord Database Reset Script
# Completely resets the database and optionally seeds it with example data

set -e

echo "🔄 YurtCord Database Reset"
echo "=========================="

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

echo -e "${RED}⚠️  WARNING: This will DELETE ALL DATA in the database!${NC}"
echo -e "${YELLOW}   All users, guilds, messages, and other data will be permanently removed.${NC}"
echo ""
read -p "Are you absolutely sure you want to continue? (yes/N) " -r
echo
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo "Cancelled."
    exit 0
fi

echo ""
echo -e "${BLUE}🗑️  Stopping all services...${NC}"
docker-compose down

echo -e "${BLUE}💥 Removing database volume...${NC}"
docker volume rm yurtcord_postgres_data 2>/dev/null || true

echo -e "${BLUE}🚀 Starting PostgreSQL...${NC}"
docker-compose up -d postgres

echo "Waiting for PostgreSQL to be ready..."
sleep 8

echo -e "${GREEN}✅ Database reset complete!${NC}"
echo ""

# Ask if user wants to seed
read -p "Do you want to seed the database with example data? (Y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Nn]$ ]]; then
    echo ""
    echo -e "${BLUE}🌱 Seeding database...${NC}"

    cd Backend/YurtCord.API
    export SEED_DATABASE=true
    timeout 30s dotnet run || true
    cd ../..

    echo ""
    echo -e "${GREEN}✅ Database seeded successfully!${NC}"
    echo ""
    echo -e "${BLUE}📝 Test accounts:${NC}"
    echo "  admin@yurtcord.com    | Password: Admin123!"
    echo "  alice@example.com     | Password: Password123!"
    echo "  bob@example.com       | Password: Password123!"
    echo "  charlie@example.com   | Password: Password123!"
    echo "  diana@example.com     | Password: Password123!"
else
    echo -e "${BLUE}✅ Database reset complete (empty database)${NC}"
fi

echo ""
echo -e "${GREEN}🎉 Ready to start the application!${NC}"
