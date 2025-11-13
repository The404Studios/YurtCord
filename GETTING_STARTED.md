# 🚀 Getting Started with YurtCord

Welcome to YurtCord! This guide will help you get up and running in minutes.

## 📋 What You Have

A complete, production-ready Discord clone with:

- ✅ **Beautiful React Frontend** - Discord-like UI with animations
- ✅ **Powerful .NET Backend** - RESTful API with SignalR WebSockets
- ✅ **Self-Contained Mode** - No Docker required!
- ✅ **Complete Feature Set** - Guilds, channels, messages, reactions, roles
- ✅ **Modern Stack** - React, TypeScript, Tailwind, Redux, .NET 8

## 🎯 Quick Start (Easiest Way)

### Option 1: Self-Contained Mode (No Docker!)

The **easiest** way to run YurtCord. Everything runs in one command!

```bash
# 1. Start Backend (auto-uses SQLite if PostgreSQL unavailable)
cd Backend/YurtCord.API
dotnet run

# 2. In a new terminal, start Frontend
cd Frontend
npm install
npm run dev
```

**That's it!** Open your browser:
- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:5000/swagger

The backend will automatically:
- Create a SQLite database at `./Data/yurtcord.db`
- Seed test accounts (admin, alice, bob, etc.)
- Use in-memory caching
- Store uploads in `./Data/uploads`

### Option 2: Docker Compose (Full Stack)

For production-like environment with PostgreSQL, Redis, and MinIO:

```bash
# Start everything with one command
docker-compose up -d

# Watch logs
docker-compose logs -f
```

Access:
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5000/swagger
- **MinIO Console**: http://localhost:9001 (minioadmin/minioadmin)

## 🔐 Test Accounts

Pre-seeded accounts you can use immediately:

| Username | Email | Password | Role |
|----------|-------|----------|------|
| admin | admin@yurtcord.com | Admin123! | Administrator |
| alice | alice@example.com | Password123! | User |
| bob | bob@example.com | Password123! | User (Nitro) |
| charlie | charlie@example.com | Password123! | User |
| diana | diana@example.com | Password123! | User |

### Pre-created Server

"The Yurt Community" with channels:
- #general
- #announcements
- #random
- General Voice (voice channel)

## 📖 Step-by-Step Guide

### 1. Register a New Account

1. Open http://localhost:5173
2. Click "Register"
3. Fill in:
   - Username: `yourname`
   - Email: `your@email.com`
   - Password: `SecurePass123!`
4. Click "Continue"

You're automatically logged in!

### 2. Explore the Pre-created Server

1. Click on "The Yurt Community" server icon (left sidebar)
2. Browse channels in the channel list
3. Click on `#general` to open the chat
4. See existing messages from test accounts

### 3. Send Your First Message

1. Type a message in the input box at the bottom
2. Press Enter or click Send
3. Your message appears instantly!

### 4. Create Your Own Server

1. Click the `+` button in the server list (left sidebar)
2. Enter server name: `My Server`
3. Click Create
4. Your new server appears!

### 5. Create Channels

1. Click the dropdown arrow next to your server name
2. Select "Create Channel"
3. Choose type: Text or Voice
4. Name it: `my-channel`
5. Click Create

### 6. Invite Friends

1. Click "Invite" in your server
2. Copy the invite link
3. Share with friends!

## 🎨 Features to Try

### Text Channels
- Send messages
- @ Mention users (type `@username`)
- Add emoji reactions
- Upload images
- Edit/delete messages

### Voice Channels
- Click on a voice channel
- UI appears (WebRTC integration ready)
- See who's connected

### User Profile
- Click on your avatar (bottom left)
- View your profile
- Change status

### Server Management
- Right-click server icon
- Server Settings
- Manage roles
- View audit log

### Member List
- See online/offline members (right sidebar)
- Click member for profile
- DM a member

## 🛠️ Configuration

### Backend Configuration

Edit `Backend/YurtCord.API/appsettings.json`:

```json
{
  "EmbeddedMode": {
    "Enabled": false,        // Force embedded mode
    "AutoDetect": true,      // Auto-switch to SQLite if PostgreSQL down
    "DatabasePath": "./Data/yurtcord.db",
    "FileStoragePath": "./Data/uploads"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=yurtcord;..."
  },
  "Jwt": {
    "Secret": "YOUR_SECRET_KEY_HERE_MIN_256_BITS",
    "Issuer": "YurtCord",
    "Audience": "YurtCordUsers"
  }
}
```

### Frontend Configuration

Edit `Frontend/.env`:

```env
VITE_API_URL=http://localhost:5000
VITE_GATEWAY_URL=http://localhost:5000/gateway
VITE_APP_NAME=YurtCord
```

## 🔄 Switching Modes

### From Self-Contained to Docker

```bash
# Stop dotnet run (Ctrl+C)

# Start Docker services
docker-compose up -d postgres redis minio

# Restart backend (now uses PostgreSQL)
cd Backend/YurtCord.API
dotnet run
```

Backend automatically detects PostgreSQL and switches!

### From Docker to Self-Contained

```bash
# Stop Docker services
docker-compose down

# Restart backend (auto-switches to SQLite)
cd Backend/YurtCord.API
dotnet run
```

## 🐛 Troubleshooting

### Backend won't start

```bash
# Check if port 5000 is in use
netstat -ano | findstr :5000

# Or use different port
dotnet run --urls="http://localhost:5001"
```

### Frontend won't start

```bash
# Clear and reinstall
cd Frontend
rm -rf node_modules package-lock.json
npm install
npm run dev
```

### Database errors

```bash
# Reset SQLite database
rm Backend/YurtCord.API/Data/yurtcord.db*

# Or reset PostgreSQL
docker-compose down -v
docker-compose up -d
```

### Can't login

1. Check backend is running: http://localhost:5000/health
2. Check frontend API URL in browser console (F12)
3. Clear browser cache and cookies
4. Try test account: alice@example.com / Password123!

### API connection failed

1. Ensure backend URL is correct in Frontend/.env
2. Check CORS is enabled in backend (it is by default)
3. Check browser console (F12) for errors
4. Verify backend is running: `curl http://localhost:5000/health`

## 📚 Next Steps

### For Developers

1. **Read Architecture**: [ARCHITECTURE.md](ARCHITECTURE.md)
2. **API Documentation**: http://localhost:5000/swagger
3. **Embedded Mode Guide**: [EMBEDDED_MODE.md](EMBEDDED_MODE.md)
4. **Frontend README**: [Frontend/README.md](Frontend/README.md)

### For Users

1. **Customize theme** in Frontend/src/styles/index.css
2. **Add custom emojis** via Server Settings
3. **Create roles** with custom permissions
4. **Set up webhooks** for integrations

### Add Features

1. **Implement WebRTC** for voice/video
2. **Add bot support** via API
3. **Create mobile app** with React Native
4. **Add search functionality**
5. **Implement threads** for organized discussions

## 🎯 Development Workflow

### Backend Development

```bash
cd Backend/YurtCord.API

# Run with hot reload
dotnet watch run

# Make changes to .cs files
# App automatically restarts!
```

### Frontend Development

```bash
cd Frontend

# Vite dev server (hot reload enabled)
npm run dev

# Make changes to .tsx files
# Browser automatically updates!
```

### Database Migrations

```bash
cd Backend/YurtCord.API

# Create migration
dotnet ef migrations add MyMigration

# Apply migration
dotnet ef database update

# Rollback
dotnet ef database update PreviousMigration
```

## 🚀 Deployment

### Deploy Backend

```bash
cd Backend/YurtCord.API

# Publish for Windows
dotnet publish -c Release -r win-x64

# Publish for Linux
dotnet publish -c Release -r linux-x64

# Output in bin/Release/net8.0/[runtime]/publish/
```

### Deploy Frontend

```bash
cd Frontend

# Build production bundle
npm run build

# Output in dist/
# Deploy dist/ to any static host (Vercel, Netlify, etc.)
```

### Docker Deployment

```bash
# Build and start
docker-compose up -d --build

# Scale services
docker-compose up -d --scale api=3
```

## 📊 Monitoring

### Check Health

```bash
# Backend health
curl http://localhost:5000/health

# Database status
curl http://localhost:5000/health/db

# Check logs
docker-compose logs -f api
```

### Performance

- Frontend: React DevTools
- Backend: Serilog logs
- Database: pgAdmin or Azure Data Studio

## 🎉 You're Ready!

You now have a complete Discord clone running!

- Beautiful UI ✨
- Real-time messaging 💬
- Full authentication 🔐
- Rich features 🎨
- Easy deployment 🚀

**Enjoy YurtCord!**

---

## 📞 Need Help?

- **Documentation**: Check README_NEW.md
- **Issues**: GitHub Issues
- **API Docs**: http://localhost:5000/swagger

---

Made with ❤️ by The404Studios
