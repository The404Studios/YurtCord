# YurtCord Quick Start Script (PowerShell)
# This script starts both Backend and Frontend in embedded mode

Write-Host "🚀 Starting YurtCord..." -ForegroundColor Cyan
Write-Host ""

# Check if .NET is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "⚠️  .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Yellow
    Write-Host "Download from: https://dotnet.microsoft.com/download"
    exit 1
}

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "✓ Node.js found: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Node.js not found. Please install Node.js 18+" -ForegroundColor Yellow
    Write-Host "Download from: https://nodejs.org/"
    exit 1
}

Write-Host ""

# Install Frontend dependencies if needed
if (-not (Test-Path "Frontend\node_modules")) {
    Write-Host "📦 Installing Frontend dependencies..." -ForegroundColor Blue
    Push-Location Frontend
    npm install
    Pop-Location
    Write-Host "✓ Frontend dependencies installed" -ForegroundColor Green
    Write-Host ""
}

# Create .env file if it doesn't exist
if (-not (Test-Path "Frontend\.env")) {
    Write-Host "📝 Creating Frontend .env file..." -ForegroundColor Blue
    Copy-Item "Frontend\.env.example" "Frontend\.env"
    Write-Host "✓ Frontend .env created" -ForegroundColor Green
    Write-Host ""
}

Write-Host "🔧 Starting Backend API (Embedded Mode)..." -ForegroundColor Blue
Write-Host "   Database: SQLite (auto-created)" -ForegroundColor Gray
Write-Host "   API: http://localhost:5000" -ForegroundColor Gray
Write-Host "   Swagger: http://localhost:5000/swagger" -ForegroundColor Gray
Write-Host ""

# Start Backend in background
Push-Location Backend\YurtCord.API
$backendJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    dotnet run 2>&1 | Out-File -FilePath "$env:TEMP\yurtcord-backend.log" -Append
}
Pop-Location

Write-Host "✓ Backend started (Job ID: $($backendJob.Id))" -ForegroundColor Green
Write-Host ""

# Wait for backend to be ready
Write-Host "⏳ Waiting for Backend to be ready..." -ForegroundColor Blue
$maxAttempts = 30
$attempt = 0
$backendReady = $false

while ($attempt -lt $maxAttempts) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 1 -UseBasicParsing -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ Backend is ready!" -ForegroundColor Green
            Write-Host ""
            $backendReady = $true
            break
        }
    } catch {
        # Continue waiting
    }

    $attempt++
    Start-Sleep -Seconds 1
}

if (-not $backendReady) {
    Write-Host "⚠️  Backend taking longer than expected. Check logs: Get-Content $env:TEMP\yurtcord-backend.log -Tail 50" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "🎨 Starting Frontend..." -ForegroundColor Blue
Write-Host "   Frontend: http://localhost:5173" -ForegroundColor Gray
Write-Host ""

# Start Frontend in background
Push-Location Frontend
$frontendJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    npm run dev 2>&1
}
Pop-Location

Write-Host "✓ Frontend started (Job ID: $($frontendJob.Id))" -ForegroundColor Green
Write-Host ""

# Save job IDs for cleanup
$backendJob.Id | Out-File "$env:TEMP\yurtcord-backend-job.txt"
$frontendJob.Id | Out-File "$env:TEMP\yurtcord-frontend-job.txt"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host "✨ YurtCord is running!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host ""
Write-Host "📍 Access Points:" -ForegroundColor Blue
Write-Host "   🌐 Frontend:  http://localhost:5173"
Write-Host "   🔧 API:       http://localhost:5000"
Write-Host "   📚 Swagger:   http://localhost:5000/swagger"
Write-Host ""
Write-Host "🔐 Test Accounts:" -ForegroundColor Blue
Write-Host "   Email: alice@example.com"
Write-Host "   Password: Password123!"
Write-Host ""
Write-Host "📊 View Logs:" -ForegroundColor Blue
Write-Host "   Backend:  Get-Content `$env:TEMP\yurtcord-backend.log -Tail 50 -Wait"
Write-Host "   Frontend: Receive-Job $($frontendJob.Id)"
Write-Host ""
Write-Host "💡 To stop YurtCord, run: .\scripts\stop.ps1" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C to stop all services" -ForegroundColor Green
Write-Host ""

# Wait for user to press Ctrl+C
try {
    while ($true) {
        Start-Sleep -Seconds 1

        # Check if jobs are still running
        if ((Get-Job -Id $backendJob.Id).State -eq 'Failed') {
            Write-Host "⚠️  Backend job failed. Check logs." -ForegroundColor Red
            break
        }
        if ((Get-Job -Id $frontendJob.Id).State -eq 'Failed') {
            Write-Host "⚠️  Frontend job failed. Check logs." -ForegroundColor Red
            break
        }
    }
} finally {
    Write-Host ""
    Write-Host "🛑 Stopping YurtCord..." -ForegroundColor Yellow

    # Stop jobs
    Stop-Job -Id $backendJob.Id -ErrorAction SilentlyContinue
    Stop-Job -Id $frontendJob.Id -ErrorAction SilentlyContinue
    Remove-Job -Id $backendJob.Id -ErrorAction SilentlyContinue
    Remove-Job -Id $frontendJob.Id -ErrorAction SilentlyContinue

    # Clean up temp files
    Remove-Item "$env:TEMP\yurtcord-backend-job.txt" -ErrorAction SilentlyContinue
    Remove-Item "$env:TEMP\yurtcord-frontend-job.txt" -ErrorAction SilentlyContinue

    Write-Host "✓ YurtCord stopped" -ForegroundColor Green
}
