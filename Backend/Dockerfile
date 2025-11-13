FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY YurtCord.sln ./
COPY YurtCord.Core/YurtCord.Core.csproj YurtCord.Core/
COPY YurtCord.Infrastructure/YurtCord.Infrastructure.csproj YurtCord.Infrastructure/
COPY YurtCord.Application/YurtCord.Application.csproj YurtCord.Application/
COPY YurtCord.API/YurtCord.API.csproj YurtCord.API/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build and publish
WORKDIR /src/YurtCord.API
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

ENTRYPOINT ["dotnet", "YurtCord.API.dll"]
