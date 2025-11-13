#!/bin/bash
echo "🔄 Restoring NuGet packages for YurtCord..."

cd /home/user/YurtCord/Backend

# Clean all projects
echo "Cleaning previous builds..."
dotnet clean

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore

# Build to verify
echo "Building solution..."
dotnet build --no-restore

echo "✅ Done!"
