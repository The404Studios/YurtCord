# 🎮 YurtCord - Open Source Discord Clone

<div align="center">

![YurtCord Logo](https://via.placeholder.com/200x200/5865f2/ffffff?text=YurtCord)

**A fully-featured, production-ready Discord clone built with .NET 8 and React**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)](https://reactjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791?logo=postgresql)](https://www.postgresql.org/)

[Features](#-features) • [Quick Start](#-quick-start) • [Documentation](#-documentation) • [Contributing](#-contributing)

</div>

---

## 📖 About

YurtCord is a comprehensive, open-source Discord alternative that provides real-time text chat, voice/video communication, and server management. Built with enterprise-grade architecture and modern technologies, it's perfect for self-hosting, learning, or building upon.

### ✨ Highlights

- 🚀 **Self-Contained Mode**: No Docker required! Runs with SQLite in one command
- 🏗️ **Clean Architecture**: Separation of concerns with 4-layer architecture
- 🔒 **Secure**: JWT authentication, BCrypt hashing, rate limiting
- ⚡ **Real-time**: SignalR WebSocket for instant messaging and events
- 🎤 **Voice/Video**: WebRTC implementation for peer-to-peer communication
- 📱 **Modern UI**: Discord-like interface built with React and TailwindCSS
- 🐳 **Docker Ready**: Optional Docker Compose for production deployment
- 📚 **Well Documented**: Comprehensive guides for developers and users

---

## 🎯 Features

### 💬 Messaging
- ✅ Real-time text messaging via WebSocket
- ✅ Message editing and deletion
- ✅ Emoji reactions (add/remove)
- ✅ Message pinning
- ✅ @ Mentions support
- ✅ File attachments (coming soon)
- ✅ Rich embeds (coming soon)
- ✅ Markdown formatting (coming soon)

### 🎤 Voice & Video
- ✅ Voice channels with WebRTC
- ✅ Video calling
- ✅ Screen sharing
- ✅ Mute/deafen controls
- ✅ Speaking indicators
- ✅ User limit per channel
- ✅ Bitrate configuration

### 🏰 Server (Guild) Management
- ✅ Create and manage servers
- ✅ Text, voice, and category channels
- ✅ Role-based permissions (41 flags)
- ✅ Member management
- ✅ Server invites (coming soon)
- ✅ Audit logs (coming soon)
- ✅ Custom emojis (coming soon)

### 👤 User Features
- ✅ User profiles with avatars and bios
- ✅ Custom status (online/idle/dnd/invisible)
- ✅ Rich presence
- ✅ Direct messages
- ✅ Friend system (coming soon)
- ✅ Notification settings (coming soon)

### 🔐 Security & Performance
- ✅ JWT authentication (7-day tokens)
- ✅ BCrypt password hashing (cost 12)
- ✅ Rate limiting (100 req/min)
- ✅ Global error handling
- ✅ Request/response logging
- ✅ Health check endpoints
- ✅ CORS configuration

### 🛠️ Developer Tools
- ✅ Complete REST API (40+ endpoints)
- ✅ Interactive Swagger documentation
- ✅ Database seeding with test data
- ✅ Development scripts
- ✅ Comprehensive guides
- ✅ Example client implementations

---

## 🚀 Quick Start

### Prerequisites

- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download)
- **Node.js 18+** - [Download here](https://nodejs.org/)
- **Docker** (optional) - Only needed for production deployment

### ⚡ 10-Second Setup (Embedded Mode - Recommended!)

**No Docker required!** Everything runs self-contained with SQLite.

```bash
# Clone the repository
git clone https://github.com/The404Studios/YurtCord.git
cd YurtCord

# Run the startup script for your platform
# Windows PowerShell:
.\scripts\start.ps1

# Windows CMD:
scripts\start.bat

# Linux/macOS:
./scripts/start.sh
```

**That's it!** The script will:
- ✓ Check for .NET SDK and Node.js
- ✓ Install dependencies automatically
- ✓ Start Backend with SQLite (no database setup needed!)
- ✓ Start Frontend dev server
- ✓ Open at http://localhost:5173

**Login with:**
- **Email**: `alice@example.com`
- **Password**: `Password123!`

### 🐳 Docker Setup (Production)

For production deployment with PostgreSQL, Redis, and MinIO:

```bash
# Clone and start
git clone https://github.com/The404Studios/YurtCord.git
cd YurtCord
docker-compose up -d
```

**Login with:**
- **Email**: `admin@yurtcord.com`
- **Password**: `Admin123!`

### Access Points

**Embedded Mode (Development):**

| Service | URL | Description |
|---------|-----|-------------|
| 🌐 Frontend | http://localhost:5173 | Main application (Vite dev server) |
| 🔌 Backend API | http://localhost:5000 | REST API |
| 📖 API Docs | http://localhost:5000/swagger | Interactive documentation |
| 💓 Health Check | http://localhost:5000/health | Service status |

**Docker Mode (Production):**

| Service | URL | Description |
|---------|-----|-------------|
| 🌐 Frontend | http://localhost:3000 | Main application |
| 🔌 Backend API | http://localhost:5000 | REST API |
| 📖 API Docs | http://localhost:5000/swagger | Interactive documentation |
| 💓 Health Check | http://localhost:5000/health | Service status |
| 📊 Grafana | http://localhost:3001 | Monitoring dashboards |
| 🗄️ MinIO | http://localhost:9001 | Object storage admin |

---

## 🏗️ Architecture

### Technology Stack

#### Backend
- **.NET 8** - Modern, high-performance framework
- **ASP.NET Core** - REST API + SignalR
- **Entity Framework Core** - ORM for PostgreSQL
- **Serilog** - Structured logging
- **BCrypt.Net** - Password hashing
- **JWT Bearer** - Authentication

#### Frontend
- **React 18** - UI library
- **TypeScript** - Type safety
- **Redux Toolkit** - State management
- **Vite** - Build tool
- **TailwindCSS** - Styling
- **SignalR Client** - Real-time connection

#### Infrastructure
- **PostgreSQL 15** - Primary database
- **Redis** - Caching and pub/sub
- **MinIO** - S3-compatible object storage
- **Prometheus** - Metrics collection
- **Grafana** - Monitoring dashboards
- **Nginx** - Reverse proxy

### Clean Architecture

```
┌─────────────────────────────────────────────┐
│          Presentation Layer                 │
│  (Controllers, Hubs, Middleware)            │
│           YurtCord.API                      │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│        Application Layer                    │
│  (Services, Business Logic)                 │
│         YurtCord.Application                │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│            Domain Layer                     │
│  (Entities, Interfaces, DTOs)               │
│           YurtCord.Core                     │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│       Infrastructure Layer                  │
│  (Database, External Services)              │
│       YurtCord.Infrastructure               │
└─────────────────────────────────────────────┘
```

---

## 📚 Documentation

### For Users
- **[Getting Started](GETTING_STARTED.md)** - ⭐ Complete beginner's guide with step-by-step instructions
- **[Embedded Mode Guide](EMBEDDED_MODE.md)** - Self-contained deployment without Docker
- **[Startup Scripts](scripts/README.md)** - Quick reference for all startup commands
- **[Quick Start](QUICKSTART.md)** - Fast setup guide
- **[Master Setup](MASTER_SETUP.md)** - Production deployment guide

### For Developers
- **[Development Guide](DEVELOPMENT.md)** - Complete development workflow
- **[API Documentation](API_DOCUMENTATION.md)** - REST API reference (40+ endpoints)
- **[Architecture](ARCHITECTURE.md)** - System design and patterns
- **[Voice Channels](VOICE_CHANNELS.md)** - WebRTC implementation guide
- **[Frontend README](Frontend/README.md)** - React frontend documentation
- **[Contributing Guide](CONTRIBUTING.md)** - How to contribute to YurtCord

---

## 🎮 API Examples

### Authentication

```javascript
// Register a new user
const response = await fetch('http://localhost:5000/api/auth/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    username: 'john',
    email: 'john@example.com',
    password: 'Password123!'
  })
});

const { token, user } = await response.json();
```

### Send a Message

```javascript
// Send message via REST
await fetch(`http://localhost:5000/api/channels/${channelId}/messages`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    content: 'Hello, world!'
  })
});

// Or via WebSocket
await connection.invoke('SendMessage', channelId, 'Hello, world!');
```

### Real-time Events

```javascript
// Connect to SignalR gateway
const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5000/gateway', {
    accessTokenFactory: () => token
  })
  .build();

// Listen for new messages
connection.on('MessageCreate', (message) => {
  console.log('New message:', message);
});

await connection.start();
```

---

## 🛠️ Development

### Running in Development Mode

```bash
# Terminal 1: Start infrastructure
docker-compose up postgres redis minio

# Terminal 2: Start backend with hot reload
cd Backend/YurtCord.API
dotnet watch run

# Terminal 3: Start frontend
cd Frontend
npm run dev
```

### Database Management

```bash
# Seed database with test data
./scripts/seed-database.sh

# Reset database completely
./scripts/reset-database.sh

# Create a migration
cd Backend/YurtCord.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../YurtCord.API
```

### Test Accounts (After Seeding)

| Email | Password | Role |
|-------|----------|------|
| admin@yurtcord.com | Admin123! | Administrator |
| alice@example.com | Password123! | User (Guild Owner) |
| bob@example.com | Password123! | User (Nitro) |
| charlie@example.com | Password123! | User |
| diana@example.com | Password123! | User |

---

## 🐳 Docker Deployment

### Development

```bash
docker-compose up -d
```

### Production

```bash
docker-compose -f docker-compose.master.yml up -d
```

### Kubernetes

Health check endpoints for probes:

```yaml
livenessProbe:
  httpGet:
    path: /api/health/live
    port: 5000

readinessProbe:
  httpGet:
    path: /api/health/ready
    port: 5000
```

---

## 🧪 Testing

### Run Tests

```bash
# All tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true

# Specific project
dotnet test Backend/YurtCord.Tests/
```

### Manual Testing

Use the **Swagger UI** at http://localhost:5000/swagger:
1. Register via `/api/auth/register`
2. Copy the JWT token
3. Click "Authorize" and enter: `Bearer <token>`
4. Test any endpoint

---

## 📊 Monitoring

### Health Checks

- **Basic**: `GET /api/health`
- **Detailed**: `GET /api/health/detailed`
- **Liveness**: `GET /api/health/live`
- **Readiness**: `GET /api/health/ready`

### Metrics

- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3001 (admin/admin)

### Logs

```bash
# View API logs
docker-compose logs -f api

# View all logs
docker-compose logs -f
```

---

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Write/update tests
5. Update documentation
6. Commit (`git commit -m 'feat: Add amazing feature'`)
7. Push (`git push origin feature/amazing-feature`)
8. Open a Pull Request

### Code Standards

- **Backend**: C# 12, Clean Architecture, async/await
- **Frontend**: TypeScript, React Hooks, Redux Toolkit
- **Commits**: Conventional Commits format
- **Tests**: Unit tests for business logic

---

## 🗺️ Roadmap

### v1.0 (Current)
- ✅ Real-time messaging
- ✅ Voice/video channels
- ✅ Server management
- ✅ User profiles
- ✅ REST API
- ✅ WebSocket gateway

### v1.1 (Next)
- ⬜ File attachments and uploads
- ⬜ Rich embeds
- ⬜ Server invites
- ⬜ Friend system
- ⬜ Direct message groups
- ⬜ Custom emojis

### v1.2 (Future)
- ⬜ Markdown formatting
- ⬜ Message threads
- ⬜ Forum channels
- ⬜ Audit logs
- ⬜ Advanced permissions
- ⬜ Mobile app

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Discord for the inspiration
- The .NET and React communities
- All contributors and users

---

## 📞 Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/The404Studios/YurtCord/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/The404Studios/YurtCord/discussions)
- **Documentation**: [Read the complete guides](QUICKSTART.md)

---

## 🌟 Star History

If you find YurtCord useful, please consider giving it a star! ⭐

---

<div align="center">

**Built with ❤️ by [The404Studios](https://github.com/The404Studios)**

[⬆ Back to Top](#-yurtcord---open-source-discord-clone)

</div>
