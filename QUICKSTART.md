# YurtCord - Complete Discord Clone - Quick Start Guide

## 🎯 What You Have

A **complete, production-ready Discord clone** with:

✅ **Full-featured React frontend** (Discord-like UI)
✅ **Scalable .NET backend** with microservices architecture
✅ **Real-time communication** (text, voice, video)
✅ **Master server orchestration** (Docker Compose)
✅ **One-command deployment**
✅ **PostgreSQL, Redis, MinIO** infrastructure
✅ **Monitoring** (Prometheus + Grafana)

---

## 🚀 Quick Start (30 seconds!)

###  **Option 1: Full Stack (Recommended)**

```bash
# Start everything with one command
./scripts/start-all.sh
```

**That's it!** Open http://localhost:3000

### **Option 2: Docker Compose (Production)**

```bash
# Start complete stack
docker-compose -f docker-compose.master.yml up -d

# Check status
docker-compose -f docker-compose.master.yml ps
```

### **Option 3: Development Mode**

```bash
# Terminal 1: Start infrastructure
docker-compose up -d postgres redis minio

# Terminal 2: Start backend
cd Backend/YurtCord.API
dotnet run

# Terminal 3: Start frontend
cd Frontend
npm install
npm run dev
```

---

## 📍 Access Points

Once started, access these URLs:

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | Main application |
| **Backend API** | http://localhost:5000 | REST API |
| **API Docs** | http://localhost:5000/swagger | Interactive API documentation |
| **WebSocket** | ws://localhost:5000/gateway | Real-time gateway |
| **MinIO Console** | http://localhost:9001 | File storage admin |
| **Grafana** | http://localhost:3001 | Monitoring dashboards |
| **Prometheus** | http://localhost:9090 | Metrics |

**Default Credentials:**
- MinIO: `minioadmin` / `minioadmin`
- Grafana: `admin` / `admin`

---

## 🎮 First-Time Setup

### 1. **Register a User**

Open http://localhost:3000 and click "Register"

```
Username: john
Email: john@example.com
Password: Password123!
```

### 2. **Create a Server**

Click the `+` button in the server list

```
Server Name: My First Server
Description: Welcome to YurtCord!
```

### 3. **Create Channels**

Right-click your server → "Create Channel"

```
Text Channel: general
Voice Channel: General Voice
```

### 4. **Invite Friends**

Click "Invite" → Copy link → Share with friends!

### 5. **Join Voice**

Click on a voice channel → Allow microphone → Start talking!

---

## 🏗️ Project Structure

```
YurtCord/
├── Backend/                    # .NET 8 Backend
│   ├── YurtCord.API/          # REST API + SignalR
│   ├── YurtCord.Application/  # Business Logic
│   ├── YurtCord.Core/         # Domain Entities
│   └── YurtCord.Infrastructure/ # Data Access
│
├── Frontend/                   # React + TypeScript
│   ├── src/
│   │   ├── components/        # UI Components
│   │   ├── pages/             # Pages
│   │   ├── services/          # API/Gateway clients
│   │   ├── store/             # Redux store
│   │   └── styles/            # CSS/Tailwind
│   ├── package.json
│   └── vite.config.ts
│
├── scripts/                    # Utility scripts
│   ├── start-all.sh           # Start everything
│   └── setup-frontend.sh      # Setup frontend
│
├── docker-compose.yml          # Development compose
├── docker-compose.master.yml   # Production compose
└── QUICKSTART.md               # This file
```

---

## 🎨 Features Included

### ✅ **Chat Features**
- Real-time messaging
- Rich text (Markdown)
- @ Mentions
- Emoji reactions
- File uploads
- Message editing/deleting
- Threads & replies
- Pinned messages

### ✅ **Voice & Video**
- Voice channels
- Video calling
- Screen sharing
- Mute/deafen/video controls
- Speaking indicators
- High-quality audio (48kHz)

### ✅ **Server Features**
- Create/manage servers
- Text/Voice/Forum channels
- Channel categories
- Roles & permissions (41 flags)
- Member management
- Server invites
- Server settings

### ✅ **Social Features**
- Friends system
- Direct messages
- Group DMs
- User profiles
- User status (online/idle/dnd/offline)
- Custom status
- Rich presence

### ✅ **Moderation**
- Ban/Kick members
- Timeout (mute)
- Delete messages
- Audit logs
- Permission system

---

## 🔧 Configuration

### Environment Variables

**Backend (.env):**
```env
DATABASE_URL=postgresql://yurtcord:password@postgres:5432/yurtcord
REDIS_URL=redis://redis:6379
MINIO_ENDPOINT=minio:9000
JWT_SECRET=your-secret-key-here
```

**Frontend (.env):**
```env
VITE_API_URL=http://localhost:5000
VITE_GATEWAY_URL=http://localhost:5000/gateway
```

### Changing Ports

Edit `docker-compose.master.yml`:

```yaml
services:
  frontend:
    ports:
      - "8080:80"  # Change 8080 to your desired port

  api:
    ports:
      - "5001:5000"  # Change 5001 to your desired port
```

---

## 🐛 Troubleshooting

### **Frontend won't start**

```bash
cd Frontend
npm install
npm run dev
```

### **Backend won't start**

```bash
cd Backend/YurtCord.API
dotnet restore
dotnet run
```

### **Database connection issues**

```bash
# Reset database
docker-compose down -v
docker-compose up -d postgres

# Wait 10 seconds
sleep 10

# Restart API
cd Backend/YurtCord.API
dotnet run
```

### **Voice not working**

1. **Check microphone permissions** in browser
2. **Try different browser** (Chrome recommended)
3. **Check STUN/TURN servers** in browser console
4. **Verify WebSocket connection** (F12 → Network tab)

### **Can't upload files**

1. **Check MinIO is running**: `docker ps | grep minio`
2. **Access MinIO console**: http://localhost:9001
3. **Check bucket exists**: Should see `yurtcord-media`

---

## 📊 Monitoring

### **Check Service Health**

```bash
# All services status
docker-compose -f docker-compose.master.yml ps

# Check logs
docker-compose -f docker-compose.master.yml logs -f api

# Check resource usage
docker stats
```

### **Grafana Dashboards**

Open http://localhost:3001

- **System Overview**: CPU, Memory, Disk
- **API Metrics**: Requests, Latency, Errors
- **Database**: Connections, Queries
- **Redis**: Commands, Memory

---

## 🚀 Production Deployment

### **1. Set Environment Variables**

```bash
# Create .env file
cat > .env <<EOF
POSTGRES_PASSWORD=secure-random-password
REDIS_PASSWORD=secure-random-password
JWT_SECRET=secure-random-jwt-secret-min-256-bits
MINIO_ACCESS_KEY=admin
MINIO_SECRET_KEY=secure-random-key
GRAFANA_PASSWORD=admin-password
EOF
```

### **2. Enable SSL/TLS**

Place SSL certificates in `nginx/ssl/`:

```
nginx/ssl/
├── cert.pem
└── key.pem
```

Update `nginx.conf` to use HTTPS.

### **3. Deploy**

```bash
# Build and start
docker-compose -f docker-compose.master.yml up -d --build

# Check status
docker-compose -f docker-compose.master.yml ps

# View logs
docker-compose -f docker-compose.master.yml logs -f
```

### **4. Domain Setup**

Point your domain to your server IP:

```
A    @           your.server.ip
A    api         your.server.ip
CNAME www        @
```

Update `.env`:

```env
API_URL=https://api.yourdomain.com
FRONTEND_URL=https://yourdomain.com
```

---

## 🔒 Security Checklist

Before going to production:

- [ ] Change all default passwords
- [ ] Generate strong JWT secret (256+ bits)
- [ ] Enable HTTPS/TLS
- [ ] Configure CORS properly
- [ ] Set up firewall rules
- [ ] Enable rate limiting
- [ ] Configure backup strategy
- [ ] Set up monitoring alerts
- [ ] Review audit logs regularly

---

## 📚 Documentation

- **API Documentation**: http://localhost:5000/swagger
- **Architecture Guide**: [ARCHITECTURE.md](ARCHITECTURE.md)
- **Voice Channels**: [VOICE_CHANNELS.md](VOICE_CHANNELS.md)
- **Master Setup**: [MASTER_SETUP.md](MASTER_SETUP.md)

---

## 🤝 Contributing

Want to add features? Here's how:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

---

## 📞 Support

- **GitHub Issues**: https://github.com/The404Studios/YurtCord/issues
- **Documentation**: https://docs.yurtcord.com
- **Email**: support@yurtcord.com

---

## 🎉 You're Ready!

YurtCord is now running! Here's what to do next:

1. ✅ Open http://localhost:3000
2. ✅ Register an account
3. ✅ Create a server
4. ✅ Invite friends
5. ✅ Start chatting and voice calling!

**Enjoy your Discord clone! 🚀**

---

**Built with ❤️ by The404Studios**

MIT License © 2025
