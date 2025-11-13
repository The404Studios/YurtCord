# YurtCord Stop Script (PowerShell)
# This script stops both Backend and Frontend services

Write-Host "🛑 Stopping YurtCord..." -ForegroundColor Yellow
Write-Host ""

# Read job IDs from temp files
$backendJobFile = "$env:TEMP\yurtcord-backend-job.txt"
$frontendJobFile = "$env:TEMP\yurtcord-frontend-job.txt"

$stopped = $false

# Stop backend job
if (Test-Path $backendJobFile) {
    $backendJobId = Get-Content $backendJobFile
    try {
        Stop-Job -Id $backendJobId -ErrorAction SilentlyContinue
        Remove-Job -Id $backendJobId -ErrorAction SilentlyContinue
        Write-Host "✓ Backend stopped" -ForegroundColor Green
        Remove-Item $backendJobFile
        $stopped = $true
    } catch {
        Write-Host "⚠️  Could not stop backend job" -ForegroundColor Yellow
    }
}

# Stop frontend job
if (Test-Path $frontendJobFile) {
    $frontendJobId = Get-Content $frontendJobFile
    try {
        Stop-Job -Id $frontendJobId -ErrorAction SilentlyContinue
        Remove-Job -Id $frontendJobId -ErrorAction SilentlyContinue
        Write-Host "✓ Frontend stopped" -ForegroundColor Green
        Remove-Item $frontendJobFile
        $stopped = $true
    } catch {
        Write-Host "⚠️  Could not stop frontend job" -ForegroundColor Yellow
    }
}

# Also try to kill processes on ports 5000 and 5173
Write-Host ""
Write-Host "🔍 Checking for processes on ports 5000 and 5173..." -ForegroundColor Blue

# Kill process on port 5000 (Backend)
$backendProcess = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -First 1
if ($backendProcess) {
    try {
        Stop-Process -Id $backendProcess -Force
        Write-Host "✓ Killed process on port 5000" -ForegroundColor Green
        $stopped = $true
    } catch {
        Write-Host "⚠️  Could not kill process on port 5000" -ForegroundColor Yellow
    }
}

# Kill process on port 5173 (Frontend)
$frontendProcess = Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -First 1
if ($frontendProcess) {
    try {
        Stop-Process -Id $frontendProcess -Force
        Write-Host "✓ Killed process on port 5173" -ForegroundColor Green
        $stopped = $true
    } catch {
        Write-Host "⚠️  Could not kill process on port 5173" -ForegroundColor Yellow
    }
}

Write-Host ""
if ($stopped) {
    Write-Host "✨ YurtCord stopped successfully!" -ForegroundColor Green
} else {
    Write-Host "ℹ️  No running YurtCord processes found" -ForegroundColor Cyan
}
