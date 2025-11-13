# 🚀 YurtCord Embedded Mode - Self-Contained Operation

YurtCord now includes a **completely self-contained embedded mode** that eliminates the need for external Docker containers, PostgreSQL, Redis, or MinIO!

## ✨ What is Embedded Mode?

Embedded Mode allows YurtCord to run as a **single standalone application** with all dependencies bundled inside:

- **SQLite Database** instead of PostgreSQL (embedded, file-based)
- **In-Memory Caching** instead of Redis
- **Local File Storage** instead of MinIO

## 🎯 Key Benefits

✅ **Zero Setup** - Just run `dotnet run`, no Docker or services needed
✅ **Auto-Detection** - Automatically switches to embedded mode if PostgreSQL is unavailable
✅ **Perfect for Development** - Quick start for coding and testing
✅ **Single Executable** - Deploy as one standalone application
✅ **No External Dependencies** - Works offline, works everywhere
✅ **Fallback Mode** - Gracefully handles when external services are down

## 🚀 Quick Start

### Option 1: Auto-Detection (Recommended)

The application **automatically detects** if PostgreSQL is available and switches to embedded mode if not:

```bash
cd Backend/YurtCord.API
dotnet run
```

That's it! You'll see:

```
[INF] PostgreSQL not available. Automatically switching to embedded mode (SQLite)
[INF] 🚀 Starting in EMBEDDED MODE (self-contained)
[INF]    Database: SQLite at ./Data/yurtcord.db
[INF]    File Storage: Local at ./Data/uploads
[INF]    Cache: In-Memory
[INF] YurtCord API starting on http://localhost:5000
```

### Option 2: Force Embedded Mode

Edit `appsettings.json`:

```json
{
  "EmbeddedMode": {
    "Enabled": true,
    "AutoDetect": true
  }
}
```

### Option 3: Use Environment Variables

```bash
export EMBEDDEDMODE__ENABLED=true
dotnet run
```

## ⚙️ Configuration

Configure embedded mode in `appsettings.json`:

```json
{
  "EmbeddedMode": {
    // Force embedded mode on (true) or off (false)
    "Enabled": false,

    // Auto-detect: Switch to embedded if PostgreSQL unavailable
    "AutoDetect": true,

    // SQLite database file location
    "DatabasePath": "./Data/yurtcord.db",

    // Local file upload storage location
    "FileStoragePath": "./Data/uploads"
  }
}
```

## 📊 Comparison: External vs Embedded

| Feature | External Mode | Embedded Mode |
|---------|---------------|---------------|
| **Database** | PostgreSQL (Docker) | SQLite (file-based) |
| **Cache** | Redis (Docker) | In-Memory |
| **File Storage** | MinIO (Docker) | Local Filesystem |
| **Setup Required** | Docker Compose | None |
| **Startup Time** | ~30 seconds | Instant |
| **Resource Usage** | ~1GB RAM | ~200MB RAM |
| **Production Ready** | ✅ Yes | ⚠️ Development/Testing |
| **Scalability** | Horizontal | Single Instance |
| **Best For** | Production, Multi-user | Development, Testing, Small deployments |

## 🎮 Usage Examples

### Development Workflow

```bash
# 1. Clone repository
git clone https://github.com/The404Studios/YurtCord.git
cd YurtCord/Backend/YurtCord.API

# 2. Run immediately - no setup needed!
dotnet run

# 3. Access Swagger
http://localhost:5000/swagger
```

### Testing Without Docker

```bash
# Run tests without external dependencies
cd Backend/YurtCord.API
dotnet run --environment=Development

# The app automatically uses SQLite
# Database created at: ./Data/yurtcord.db
# Uploads stored at: ./Data/uploads
```

### Portable Deployment

```bash
# Publish as self-contained executable
dotnet publish -c Release -r win-x64 --self-contained

# Copy the publish folder to any Windows machine
# Run YurtCord.API.exe - no dependencies needed!
```

## 🔄 Switching Between Modes

### Auto-Detection (Default Behavior)

The application checks for PostgreSQL availability at startup:

1. **PostgreSQL available** → Uses External Mode (PostgreSQL, Redis, MinIO)
2. **PostgreSQL unavailable** → Automatically switches to Embedded Mode (SQLite, In-Memory, Local)

### Force External Mode

Set `AutoDetect` to `false` and ensure PostgreSQL is running:

```json
{
  "EmbeddedMode": {
    "Enabled": false,
    "AutoDetect": false
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=yurtcord;..."
  }
}
```

### Force Embedded Mode

```json
{
  "EmbeddedMode": {
    "Enabled": true
  }
}
```

## 📁 Data Storage

When running in embedded mode, YurtCord creates a `Data` directory:

```
Backend/YurtCord.API/
├── Data/
│   ├── yurtcord.db           # SQLite database
│   ├── yurtcord.db-shm        # SQLite shared memory
│   ├── yurtcord.db-wal        # SQLite write-ahead log
│   └── uploads/               # File uploads
│       ├── avatars/
│       ├── attachments/
│       └── emojis/
```

## 🔍 How Auto-Detection Works

1. Application starts
2. Reads `appsettings.json` configuration
3. Checks `EmbeddedMode.AutoDetect` (default: `true`)
4. Attempts TCP connection to PostgreSQL host:port
5. **If connection succeeds** → Use PostgreSQL (External Mode)
6. **If connection fails** → Use SQLite (Embedded Mode)
7. Logs which mode is active

## ⚠️ Limitations of Embedded Mode

While embedded mode is perfect for development, be aware of limitations:

### SQLite vs PostgreSQL

- ⚠️ **Concurrency**: SQLite handles fewer concurrent writes
- ⚠️ **Performance**: PostgreSQL is faster for large datasets
- ⚠️ **Features**: Some PostgreSQL-specific features not available
- ✅ **Compatibility**: Same API, same code, same migrations

### In-Memory Cache vs Redis

- ⚠️ **Persistence**: Cache clears on restart
- ⚠️ **Distributed**: Cannot share cache across instances
- ✅ **Speed**: Actually faster than Redis for single instance

### Local Storage vs MinIO

- ⚠️ **Scalability**: Files stored on local disk only
- ⚠️ **Redundancy**: No automatic backup/replication
- ✅ **Simplicity**: No configuration needed

## 🚀 Production Recommendations

### Development/Testing
✅ **Use Embedded Mode** - Fast, simple, no setup

### Small Deployments (< 100 users)
✅ **Use Embedded Mode** - SQLite handles this well

### Production (> 100 users)
✅ **Use External Mode** - PostgreSQL, Redis, MinIO for scale

## 🐛 Troubleshooting

### "PostgreSQL not available" but I have Docker running

Check that PostgreSQL is accessible:

```bash
# Test connection
psql -h localhost -p 5432 -U yurtcord -d yurtcord

# Or check with telnet
telnet localhost 5432
```

If Docker is running but not accessible, check:
- Port forwarding: `-p 5432:5432`
- Container is healthy: `docker ps`
- Firewall settings

### Database file locked

SQLite locks the database file while running. If you see "database is locked":

1. Ensure only one instance of the app is running
2. Close any SQLite browser tools
3. Delete `.db-shm` and `.db-wal` files if stuck

### Cannot write to Data directory

Ensure the application has write permissions:

```bash
# Linux/Mac
chmod 755 ./Data

# Or run with elevated permissions (not recommended for production)
sudo dotnet run
```

## 🎯 Example Scenarios

### Scenario 1: Developer on Laptop

```bash
# No Docker installed
cd Backend/YurtCord.API
dotnet run

# ✅ Auto-detects, uses SQLite
# ✅ Can code on airplane
# ✅ Instant startup
```

### Scenario 2: Team Development with Docker

```bash
# Developer 1: Uses Docker
docker-compose up -d
dotnet run

# ✅ Connects to PostgreSQL
# ✅ Shared database

# Developer 2: No Docker
dotnet run

# ✅ Uses SQLite locally
# ✅ Independent database
```

### Scenario 3: Production Deployment

```bash
# Production server has PostgreSQL
dotnet run

# ✅ Auto-detects PostgreSQL
# ✅ Uses production database
# ✅ Robust and scalable
```

## 📚 Additional Resources

- **Main README**: [README_NEW.md](README_NEW.md)
- **Quick Start**: [QUICKSTART.md](QUICKSTART.md)
- **Architecture**: [ARCHITECTURE.md](ARCHITECTURE.md)
- **Docker Compose**: [docker-compose.yml](docker-compose.yml)

## 🎉 Summary

Embedded Mode makes YurtCord:

- **✅ Easier to start** - No setup, just run
- **✅ Easier to develop** - No Docker overhead
- **✅ Easier to test** - Clean slate every time
- **✅ Easier to deploy** - Single executable
- **✅ Easier to demo** - Works anywhere

**Just run `dotnet run` and start building!** 🚀

---

Made with ❤️ by The404Studios
