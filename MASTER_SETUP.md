# YurtCord Master Server Setup - Complete Discord Clone

## 🎯 Overview

YurtCord is now a **complete, production-ready Discord clone** with:
- ✅ Full-featured React frontend (Discord UI)
- ✅ Scalable .NET backend
- ✅ Real-time voice/video/text communication
- ✅ Master server orchestration
- ✅ One-command deployment

---

## 📁 Project Structure

```
YurtCord/
├── Backend/                           # Backend services
│   ├── YurtCord.API/                 # Main API server
│   ├── YurtCord.Application/         # Business logic
│   ├── YurtCord.Core/                # Domain entities
│   ├── YurtCord.Infrastructure/      # Data & external services
│   └── YurtCord.Gateway/             # WebSocket gateway (separate service)
│
├── Frontend/                          # React frontend
│   ├── src/
│   │   ├── components/               # UI components
│   │   │   ├── layout/              # Layout components
│   │   │   │   ├── Sidebar.tsx      # Server/channel sidebar
│   │   │   │   ├── TopBar.tsx       # Top navigation
│   │   │   │   └── UserPanel.tsx    # User info panel
│   │   │   ├── chat/                # Chat components
│   │   │   │   ├── MessageList.tsx  # Message display
│   │   │   │   ├── MessageInput.tsx # Message composer
│   │   │   │   └── UserTyping.tsx   # Typing indicator
│   │   │   ├── voice/               # Voice components
│   │   │   │   ├── VoicePanel.tsx   # Voice controls
│   │   │   │   ├── UserVoice.tsx    # Voice user card
│   │   │   │   └── VoiceControls.tsx # Mute/deafen buttons
│   │   │   ├── server/              # Server components
│   │   │   │   ├── ServerIcon.tsx   # Server icon
│   │   │   │   ├── ChannelList.tsx  # Channel list
│   │   │   │   └── ServerSettings.tsx # Server settings
│   │   │   └── common/              # Shared components
│   │   │       ├── Modal.tsx        # Modal dialog
│   │   │       ├── Button.tsx       # Button component
│   │   │       └── Input.tsx        # Input component
│   │   ├── services/                # Services
│   │   │   ├── api.ts              # API client
│   │   │   ├── gateway.ts          # WebSocket gateway
│   │   │   ├── voice.ts            # Voice client
│   │   │   └── auth.ts             # Authentication
│   │   ├── store/                   # Redux store
│   │   │   ├── slices/             # Redux slices
│   │   │   │   ├── authSlice.ts
│   │   │   │   ├── guildsSlice.ts
│   │   │   │   ├── messagesSlice.ts
│   │   │   │   └── voiceSlice.ts
│   │   │   └── store.ts            # Store configuration
│   │   ├── hooks/                   # Custom hooks
│   │   │   ├── useAuth.ts
│   │   │   ├── useGateway.ts
│   │   │   └── useVoice.ts
│   │   ├── styles/                  # Styles
│   │   │   ├── theme.ts            # Theme configuration
│   │   │   └── globals.css         # Global styles
│   │   ├── pages/                   # Pages
│   │   │   ├── Login.tsx
│   │   │   ├── Register.tsx
│   │   │   ├── Home.tsx            # Main app
│   │   │   └── Settings.tsx
│   │   ├── App.tsx                 # Root component
│   │   └── main.tsx                # Entry point
│   ├── public/                      # Static assets
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
│
├── MasterServer/                     # Master orchestration
│   ├── docker-compose.master.yml    # Full stack compose
│   ├── nginx.conf                   # Reverse proxy config
│   ├── .env.master                  # Master environment
│   └── deploy.sh                    # Deployment script
│
├── docs/                             # Documentation
│   ├── API.md                       # API documentation
│   ├── DEPLOYMENT.md                # Deployment guide
│   └── DEVELOPMENT.md               # Development guide
│
├── scripts/                          # Utility scripts
│   ├── setup.sh                     # Initial setup
│   ├── start-dev.sh                 # Start dev environment
│   └── build-prod.sh                # Build for production
│
├── docker-compose.yml               # Development compose
├── docker-compose.prod.yml          # Production compose
├── README.md                        # Main readme
└── MASTER_SETUP.md                  # This file
```

---

## 🚀 Quick Start (One Command!)

### Option 1: Full Production Stack

```bash
# Clone and start everything
git clone https://github.com/The404Studios/YurtCord.git
cd YurtCord
./scripts/setup.sh
./scripts/start-master.sh
```

**Access:**
- Frontend: http://localhost:3000
- API: http://localhost:5000
- Gateway: http://localhost:5001
- Admin Panel: http://localhost:9000

### Option 2: Development Mode

```bash
# Start development environment
./scripts/start-dev.sh

# Frontend: http://localhost:3000 (auto-reload)
# Backend: http://localhost:5000 (hot reload)
```

---

## 🏗️ Master Server Architecture

```
                    ┌─────────────────────┐
                    │   Load Balancer     │
                    │   (Nginx)           │
                    │   Port: 80/443      │
                    └──────────┬──────────┘
                               │
              ┌────────────────┴────────────────┐
              │                                  │
    ┌─────────▼─────────┐            ┌──────────▼──────────┐
    │  Frontend         │            │  API Gateway        │
    │  (React/Vite)     │            │  (ASP.NET Core)     │
    │  Port: 3000       │            │  Port: 5000         │
    └───────────────────┘            └──────────┬──────────┘
                                                 │
                               ┌─────────────────┴─────────────────┐
                               │                                   │
                    ┌──────────▼──────────┐         ┌─────────────▼──────────┐
                    │  WebSocket Gateway  │         │  Background Services   │
                    │  (SignalR)          │         │  - Email notifications │
                    │  Port: 5001         │         │  - Media processing    │
                    └──────────┬──────────┘         │  - Cleanup tasks       │
                               │                     └────────────────────────┘
                               │
              ┌────────────────┴────────────────┐
              │                                  │
    ┌─────────▼─────────┐            ┌──────────▼──────────┐
    │  PostgreSQL       │            │  Redis              │
    │  Port: 5432       │            │  Port: 6379         │
    └───────────────────┘            └─────────────────────┘
              │
    ┌─────────▼─────────┐
    │  MinIO (S3)       │
    │  Port: 9000       │
    └───────────────────┘
```

---

## 🎨 Frontend Features

### Discord-Like UI Components

#### 1. **Main Layout**
```
┌─────────────────────────────────────────────────────┐
│  Top Bar (User info, notifications, settings)       │
├───────┬─────────────────────────────────┬───────────┤
│       │                                 │           │
│ Srv   │  Channel Name          📢 🎥 📱 │  Members  │
│ List  ├─────────────────────────────────┤           │
│       │                                 │  👤 User1 │
│ 🏠    │  Message History                │  👤 User2 │
│ ⚙️    │  ┌─────────────────────┐       │  👤 User3 │
│ 🎮    │  │ User: Message text  │       │           │
│ 💬    │  └─────────────────────┘       │  Voice    │
│       │  ┌─────────────────────┐       │  ┌───────┐│
│ Chan  │  │ User: Another msg   │       │  │ 🎙️ Ch │ │
│ ▸ 📝  │  └─────────────────────┘       │  │ User4 ││
│ ▸ 🔊  │                                 │  │ User5 ││
│ ▸ 📺  │  [Type a message...]            │  └───────┘│
│       │  [😀 📷 🎁 🎵]                    │           │
└───────┴─────────────────────────────────┴───────────┘
```

#### 2. **Components Included**

**Layout:**
- ✅ Server sidebar (left)
- ✅ Channel list (middle-left)
- ✅ Chat area (center)
- ✅ Member list (right)
- ✅ User panel (bottom)

**Chat:**
- ✅ Message display with avatars
- ✅ Rich text formatting
- ✅ Emoji picker
- ✅ File upload drag-drop
- ✅ @ mentions autocomplete
- ✅ Reply/thread support
- ✅ Reactions
- ✅ Message editing/deleting

**Voice:**
- ✅ Voice panel with users
- ✅ Mute/deafen/video controls
- ✅ Speaking indicators
- ✅ Screen share button
- ✅ Audio visualization

**Server Management:**
- ✅ Create/edit servers
- ✅ Channel management
- ✅ Role management
- ✅ Member management
- ✅ Server settings
- ✅ Invite management

---

## 🔧 Configuration

### Environment Variables

Create `.env` files:

**Backend (.env):**
```env
# Database
DATABASE_URL=postgresql://yurtcord:password@postgres:5432/yurtcord

# Redis
REDIS_URL=redis://redis:6379

# MinIO
MINIO_ENDPOINT=minio:9000
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=minioadmin

# JWT
JWT_SECRET=your-super-secret-jwt-key-change-in-production
JWT_ISSUER=YurtCord
JWT_AUDIENCE=YurtCordUsers
JWT_EXPIRY_DAYS=7

# CORS
CORS_ORIGINS=http://localhost:3000,https://yourcord.com

# Features
ENABLE_REGISTRATION=true
ENABLE_EMAIL_VERIFICATION=false
ENABLE_VOICE=true
MAX_UPLOAD_SIZE_MB=100
```

**Frontend (.env):**
```env
VITE_API_URL=http://localhost:5000
VITE_GATEWAY_URL=http://localhost:5000/gateway
VITE_APP_NAME=YurtCord
VITE_MAX_FILE_SIZE=104857600
```

---

## 📦 Deployment Options

### Option 1: Docker Compose (Recommended)

```bash
# Development
docker-compose up -d

# Production
docker-compose -f docker-compose.prod.yml up -d
```

### Option 2: Kubernetes

```bash
kubectl apply -f k8s/
```

### Option 3: Manual

**Backend:**
```bash
cd Backend/YurtCord.API
dotnet publish -c Release -o out
dotnet out/YurtCord.API.dll
```

**Frontend:**
```bash
cd Frontend
npm install
npm run build
npm run preview
```

---

## 🎯 Features Checklist

### ✅ Core Features
- [x] User registration and login
- [x] JWT authentication
- [x] Password hashing
- [x] Email verification (optional)
- [x] 2FA support

### ✅ Servers (Guilds)
- [x] Create servers
- [x] Server icons and banners
- [x] Server settings
- [x] Member management
- [x] Role management
- [x] Permission system (41 flags)
- [x] Server invites
- [x] Server discovery

### ✅ Channels
- [x] Text channels
- [x] Voice channels
- [x] Video channels
- [x] Forum channels
- [x] Stage channels
- [x] Channel categories
- [x] Channel permissions
- [x] Channel settings

### ✅ Messaging
- [x] Real-time messaging
- [x] Message history
- [x] Rich text (markdown)
- [x] @ mentions
- [x] Emoji reactions
- [x] File uploads
- [x] Image/video embeds
- [x] Message editing
- [x] Message deletion
- [x] Pinned messages
- [x] Threads
- [x] Replies

### ✅ Voice & Video
- [x] Voice channels
- [x] Video calling
- [x] Screen sharing
- [x] Mute/unmute
- [x] Deafen
- [x] Speaking indicators
- [x] Voice settings
- [x] Audio quality control

### ✅ Social
- [x] Friends system
- [x] Friend requests
- [x] Direct messages
- [x] Group DMs
- [x] User profiles
- [x] User status
- [x] Rich presence
- [x] Custom status

### ✅ Moderation
- [x] Ban members
- [x] Kick members
- [x] Timeout members
- [x] Delete messages
- [x] Audit logs
- [x] Auto-moderation
- [x] Content filters

### ✅ Platform
- [x] Bot API
- [x] OAuth2
- [x] Webhooks
- [x] Slash commands
- [x] Message components
- [x] Integrations

---

## 🔐 Security

### Authentication
- JWT tokens with refresh tokens
- Bcrypt password hashing (cost: 12)
- Email verification
- 2FA (TOTP)
- Session management

### API Security
- Rate limiting (per user/IP)
- Request validation
- SQL injection prevention
- XSS protection
- CSRF protection
- CORS configuration

### Data Protection
- TLS/SSL encryption
- End-to-end encryption option
- PII encryption at rest
- Secure file uploads
- Content scanning

---

## 📊 Performance

### Optimizations
- Redis caching
- Database indexing
- Query optimization
- CDN for static assets
- Lazy loading
- Code splitting
- Bundle optimization
- Image optimization

### Scaling
- Horizontal scaling
- Load balancing
- Database replication
- Microservices ready
- WebSocket clustering

---

## 🧪 Testing

```bash
# Backend tests
cd Backend
dotnet test

# Frontend tests
cd Frontend
npm test

# E2E tests
npm run test:e2e

# Load testing
k6 run loadtest.js
```

---

## 📖 API Documentation

### Swagger UI
- Development: http://localhost:5000/swagger
- Production: https://api.yourcord.com/swagger

### GraphQL Playground (Future)
- http://localhost:5000/graphql

---

## 🎓 Development Guide

### Adding a New Feature

1. **Backend:**
```bash
# Create entity
Backend/YurtCord.Core/Entities/NewEntity.cs

# Create service
Backend/YurtCord.Application/Services/NewService.cs

# Create controller
Backend/YurtCord.API/Controllers/NewController.cs

# Add to DI
Backend/YurtCord.API/Program.cs
```

2. **Frontend:**
```bash
# Create component
Frontend/src/components/new/NewComponent.tsx

# Create service
Frontend/src/services/newService.ts

# Add to store
Frontend/src/store/slices/newSlice.ts

# Use in page
Frontend/src/pages/NewPage.tsx
```

---

## 🐛 Troubleshooting

### Common Issues

**Database connection fails:**
```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Check connection string
echo $DATABASE_URL

# Reset database
docker-compose down -v
docker-compose up -d postgres
```

**Frontend can't connect to backend:**
```bash
# Check CORS settings
# Check API_URL in .env
# Check network in Docker
docker network ls
```

**Voice not working:**
```bash
# Check WebRTC in browser console
# Check STUN/TURN servers
# Check microphone permissions
# Try different browser
```

---

## 📞 Support

- GitHub Issues: https://github.com/The404Studios/YurtCord/issues
- Documentation: https://docs.yurtcord.com
- Discord Server: https://discord.gg/yurtcord
- Email: support@yurtcord.com

---

## 🎉 Credits

Built with:
- .NET 8.0
- React 18
- TypeScript
- PostgreSQL
- Redis
- MinIO
- Docker
- Nginx

**Made with ❤️ by The404Studios**

---

## 📜 License

MIT License - See LICENSE file for details
