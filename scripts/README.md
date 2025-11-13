# YurtCord Startup Scripts

Quick reference for starting and stopping YurtCord.

## 🚀 Starting YurtCord

Choose the script that matches your operating system:

### Windows

**PowerShell (Recommended for Windows 10/11):**
```powershell
.\scripts\start.ps1
```

**Command Prompt (Batch file):**
```cmd
scripts\start.bat
```

### Linux / macOS

```bash
./scripts/start.sh
```

## 🛑 Stopping YurtCord

### Windows

**PowerShell:**
```powershell
.\scripts\stop.ps1
```

**Command Prompt:**
```cmd
scripts\stop.bat
```

### Linux / macOS

```bash
./scripts/stop.sh
```

**Or simply:** Press `Ctrl+C` in the terminal where you started YurtCord.

## 📝 What These Scripts Do

### Start Scripts

All start scripts perform the same actions:

1. ✓ **Check Dependencies** - Verify .NET SDK and Node.js are installed
2. ✓ **Install Packages** - Auto-install Frontend npm packages if needed
3. ✓ **Create Config** - Generate `.env` file from template if missing
4. ✓ **Start Backend** - Launch .NET API server in embedded mode (SQLite)
5. ✓ **Wait for Ready** - Poll health endpoint until Backend is responsive
6. ✓ **Start Frontend** - Launch Vite dev server
7. ✓ **Display Info** - Show access URLs and test accounts

**Result:** YurtCord is fully running and accessible at http://localhost:5173

### Stop Scripts

All stop scripts perform cleanup:

1. ✓ **Kill Backend** - Terminate dotnet process on port 5000
2. ✓ **Kill Frontend** - Terminate node process on port 5173
3. ✓ **Clean Temp Files** - Remove PID files and job tracking
4. ✓ **Confirm** - Display success message

## 🔧 Troubleshooting

### "Permission Denied" (Linux/macOS)

The shell scripts need execute permissions:

```bash
chmod +x scripts/*.sh
```

### "Execution Policy" Error (Windows PowerShell)

PowerShell might block scripts. To allow:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Or run with bypass:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start.ps1
```

### Scripts Won't Stop Services

If stop scripts don't work, manually kill processes:

**Windows:**
```cmd
netstat -ano | findstr :5000
taskkill /F /PID <PID>
```

**Linux/macOS:**
```bash
lsof -ti:5000 | xargs kill
lsof -ti:5173 | xargs kill
```

### Port Already in Use

If ports 5000 or 5173 are already taken:

1. Run the stop script to clean up old processes
2. Or manually kill the process using the port
3. Or change the ports in configuration files

## 📊 Log Files

### Windows (PowerShell/Batch)

- **Backend:** `%TEMP%\yurtcord-backend.log`
- **View:** `Get-Content $env:TEMP\yurtcord-backend.log -Tail 50 -Wait`

### Linux/macOS

- **Backend:** `/tmp/yurtcord-backend.log`
- **View:** `tail -f /tmp/yurtcord-backend.log`

### Frontend

Frontend runs in foreground - logs appear in the terminal/command window.

## 🎯 Quick Reference

| Action | Windows PowerShell | Windows CMD | Linux/macOS |
|--------|-------------------|-------------|-------------|
| Start | `.\scripts\start.ps1` | `scripts\start.bat` | `./scripts/start.sh` |
| Stop | `.\scripts\stop.ps1` | `scripts\stop.bat` | `./scripts/stop.sh` |
| View Backend Logs | `Get-Content $env:TEMP\yurtcord-backend.log -Tail 50` | `type %TEMP%\yurtcord-backend.log` | `tail -f /tmp/yurtcord-backend.log` |

## ℹ️ Access Points After Starting

Once started, access YurtCord at:

- 🌐 **Frontend:** http://localhost:5173
- 🔧 **API:** http://localhost:5000
- 📚 **Swagger Docs:** http://localhost:5000/swagger
- 🏥 **Health Check:** http://localhost:5000/health

## 🔐 Default Test Account

- **Email:** alice@example.com
- **Password:** Password123!

---

**Need more help?** See [GETTING_STARTED.md](../GETTING_STARTED.md) for detailed setup instructions.
