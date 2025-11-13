# YurtCord - Development Guide

Complete guide for developers working on YurtCord.

---

## Table of Contents

- [Getting Started](#getting-started)
- [Development Environment](#development-environment)
- [Database Management](#database-management)
- [Running the Application](#running-the-application)
- [Testing](#testing)
- [Code Standards](#code-standards)
- [Troubleshooting](#troubleshooting)

---

## Getting Started

### Prerequisites

- **Docker** and **Docker Compose**
- **.NET 8 SDK** (for backend development)
- **Node.js 18+** and **npm** (for frontend development)
- **Git**

### Initial Setup

```bash
# Clone the repository
git clone https://github.com/The404Studios/YurtCord.git
cd YurtCord

# Start infrastructure services
docker-compose up -d postgres redis minio

# Seed database with example data
./scripts/seed-database.sh
```

---

## Development Environment

### Backend (.NET 8)

```bash
cd Backend/YurtCord.API

# Restore dependencies
dotnet restore

# Run the API
dotnet run

# Or with hot reload
dotnet watch run
```

**API will be available at**: `http://localhost:5000`
**Swagger UI**: `http://localhost:5000/swagger`

### Frontend (React + TypeScript)

```bash
cd Frontend

# Install dependencies
npm install

# Start development server
npm run dev
```

**Frontend will be available at**: `http://localhost:3000`

---

## Database Management

### Seeding the Database

The database seeder creates example data for development:
- 5 test users with different roles
- 3 guilds (servers) with channels
- Example messages and reactions
- Roles with proper permissions

**Method 1: Using the Script**
```bash
./scripts/seed-database.sh
```

**Method 2: Environment Variable**
```bash
cd Backend/YurtCord.API
SEED_DATABASE=true dotnet run
```

**Method 3: appsettings.json**
```json
{
  "SeedDatabase": true
}
```

### Test Accounts

After seeding, you can login with these accounts:

| Email | Password | Role |
|-------|----------|------|
| `admin@yurtcord.com` | `Admin123!` | Administrator |
| `alice@example.com` | `Password123!` | Regular User |
| `bob@example.com` | `Password123!` | Regular User (Nitro) |
| `charlie@example.com` | `Password123!` | Regular User |
| `diana@example.com` | `Password123!` | Regular User |

### Resetting the Database

**Complete reset** (removes all data):
```bash
./scripts/reset-database.sh
```

This will:
1. Stop all services
2. Delete the database volume
3. Restart PostgreSQL
4. Optionally seed with example data

### Database Migrations

**Create a new migration**:
```bash
cd Backend/YurtCord.Infrastructure
dotnet ef migrations add YourMigrationName --startup-project ../YurtCord.API
```

**Apply migrations**:
```bash
cd Backend/YurtCord.API
dotnet ef database update --project ../YurtCord.Infrastructure
```

**Rollback migration**:
```bash
cd Backend/YurtCord.API
dotnet ef database update PreviousMigrationName --project ../YurtCord.Infrastructure
```

**Remove last migration** (if not applied):
```bash
cd Backend/YurtCord.Infrastructure
dotnet ef migrations remove --startup-project ../YurtCord.API
```

---

## Running the Application

### Full Stack Development

**Option 1: All-in-one script**
```bash
./scripts/start-all.sh
```

**Option 2: Manual (3 terminals)**

Terminal 1 - Infrastructure:
```bash
docker-compose up postgres redis minio
```

Terminal 2 - Backend:
```bash
cd Backend/YurtCord.API
dotnet watch run
```

Terminal 3 - Frontend:
```bash
cd Frontend
npm run dev
```

### Production Mode

```bash
docker-compose -f docker-compose.master.yml up -d
```

---

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test Backend/YurtCord.Tests/YurtCord.Tests.csproj
```

### Manual Testing

**Using Swagger UI**: http://localhost:5000/swagger

1. Register a new account via `/api/auth/register`
2. Copy the JWT token from the response
3. Click "Authorize" button in Swagger
4. Enter: `Bearer <your-token>`
5. Test all endpoints

**Using API Documentation**: See [API_DOCUMENTATION.md](API_DOCUMENTATION.md)

---

## Code Standards

### Backend (C#)

- **Use C# 12 features**: Primary constructors, pattern matching
- **Follow Clean Architecture**: Separate concerns across layers
- **Naming conventions**:
  - PascalCase for classes, methods, properties
  - camelCase for parameters, local variables
  - Prefix interfaces with `I`
- **Async/Await**: All I/O operations must be async
- **Null checking**: Use nullable reference types
- **Comments**: XML documentation for public APIs

**Example**:
```csharp
/// <summary>
/// Gets a user by their unique identifier
/// </summary>
/// <param name="userId">The user's snowflake ID</param>
/// <returns>User object or null if not found</returns>
public async Task<User?> GetUserAsync(Snowflake userId)
{
    return await _context.Users
        .Include(u => u.Presence)
        .FirstOrDefaultAsync(u => u.Id == userId);
}
```

### Frontend (TypeScript)

- **Use TypeScript**: Strong typing for all code
- **Component structure**: Functional components with hooks
- **State management**: Redux Toolkit for global state
- **Naming conventions**:
  - PascalCase for components
  - camelCase for functions, variables
  - UPPER_CASE for constants
- **File structure**:
  ```
  ComponentName/
  ├── index.tsx          # Component logic
  ├── ComponentName.tsx  # Main component
  ├── styles.module.css  # Scoped styles
  └── types.ts           # Component types
  ```

---

## Project Structure

```
YurtCord/
├── Backend/
│   ├── YurtCord.API/              # REST API + SignalR
│   │   ├── Controllers/           # API endpoints
│   │   ├── Hubs/                  # SignalR hubs
│   │   ├── Middleware/            # Custom middleware
│   │   └── Program.cs             # App configuration
│   ├── YurtCord.Application/      # Business logic
│   │   └── Services/              # Application services
│   ├── YurtCord.Core/             # Domain layer
│   │   ├── Entities/              # Domain entities
│   │   └── Common/                # Shared types
│   └── YurtCord.Infrastructure/   # Data access
│       └── Data/                  # DbContext, migrations
│
├── Frontend/
│   ├── src/
│   │   ├── components/            # Reusable UI components
│   │   ├── pages/                 # Page components
│   │   ├── services/              # API clients
│   │   ├── store/                 # Redux store
│   │   └── styles/                # Global styles
│   └── public/                    # Static assets
│
├── scripts/                       # Utility scripts
└── docs/                          # Documentation
```

---

## Environment Variables

### Backend (.env or appsettings.json)

```env
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Database=yurtcord;Username=yurtcord;Password=password

# JWT
Jwt__Secret=your-secret-key-min-256-bits
Jwt__Issuer=YurtCord
Jwt__Audience=YurtCord

# Redis (optional)
REDIS_URL=redis://localhost:6379

# MinIO (optional)
MINIO_ENDPOINT=localhost:9000
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=minioadmin

# Seeding
SEED_DATABASE=false
```

### Frontend (.env)

```env
VITE_API_URL=http://localhost:5000
VITE_GATEWAY_URL=http://localhost:5000/gateway
```

---

## Debugging

### Backend Debugging (VS Code)

**launch.json**:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/Backend/YurtCord.API/bin/Debug/net8.0/YurtCord.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/Backend/YurtCord.API",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "SEED_DATABASE": "false"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

### Database Debugging

**Connect to PostgreSQL**:
```bash
docker exec -it yurtcord-postgres psql -U yurtcord -d yurtcord
```

**Common queries**:
```sql
-- List all tables
\dt

-- Count users
SELECT COUNT(*) FROM "Users";

-- List guilds
SELECT "Id", "Name", "OwnerId" FROM "Guilds";

-- Recent messages
SELECT m."Content", u."Username"
FROM "Messages" m
JOIN "Users" u ON m."AuthorId" = u."Id"
ORDER BY m."Timestamp" DESC
LIMIT 10;
```

---

## Performance

### Database Optimization

- **Indexes**: Ensure indexes on foreign keys
- **Eager loading**: Use `.Include()` to avoid N+1 queries
- **Pagination**: Always paginate large result sets
- **Connection pooling**: Configured in connection string

### SignalR Optimization

- **Use groups**: Subscribe to specific channels/guilds
- **Backplane**: Use Redis for scaling (production)
- **Message size**: Keep messages small, use IDs not full objects

---

## Troubleshooting

### "Database does not exist"

```bash
# Reset and recreate database
./scripts/reset-database.sh
```

### "Port already in use"

```bash
# Check what's using the port
lsof -i :5000
lsof -i :3000

# Kill the process
kill -9 <PID>
```

### "Cannot connect to PostgreSQL"

```bash
# Restart PostgreSQL
docker-compose restart postgres

# Check logs
docker-compose logs postgres
```

### "JWT token invalid"

- Check if token is expired (7-day expiry)
- Verify `Jwt:Secret` matches in appsettings.json
- Clear browser localStorage and login again

### Frontend not connecting to backend

- Check `VITE_API_URL` in `.env`
- Verify CORS settings in `Program.cs`
- Check browser console for errors
- Ensure backend is running

---

## Git Workflow

### Branch Naming

- `feature/description` - New features
- `fix/description` - Bug fixes
- `refactor/description` - Code refactoring
- `docs/description` - Documentation updates

### Commit Messages

Follow conventional commits:

```
feat: Add user profile editing
fix: Resolve JWT expiration issue
refactor: Simplify permission checking
docs: Update API documentation
test: Add integration tests for messages
```

### Pull Request

1. Create a feature branch
2. Make your changes
3. Write tests
4. Update documentation
5. Create PR with description
6. Wait for review

---

## Additional Resources

- **API Documentation**: [API_DOCUMENTATION.md](API_DOCUMENTATION.md)
- **Quick Start**: [QUICKSTART.md](QUICKSTART.md)
- **Architecture**: [ARCHITECTURE.md](ARCHITECTURE.md)
- **Voice Channels**: [VOICE_CHANNELS.md](VOICE_CHANNELS.md)
- **Master Setup**: [MASTER_SETUP.md](MASTER_SETUP.md)

---

## Getting Help

- **GitHub Issues**: https://github.com/The404Studios/YurtCord/issues
- **Discussions**: https://github.com/The404Studios/YurtCord/discussions

---

**Happy coding! 🚀**

MIT License © 2025 The404Studios
