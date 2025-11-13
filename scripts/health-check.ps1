# YurtCord Health Check Script (PowerShell)
# Verifies that all components are working correctly

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🏥 YurtCord Health Check" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

$SUCCESS = 0
$WARNINGS = 0
$FAILURES = 0

# Function to print status
function Print-Status {
    param(
        [string]$Status,
        [string]$Message
    )

    if ($Status -eq "success") {
        Write-Host "✓ $Message" -ForegroundColor Green
        $script:SUCCESS++
    } elseif ($Status -eq "warning") {
        Write-Host "⚠ $Message" -ForegroundColor Yellow
        $script:WARNINGS++
    } else {
        Write-Host "✗ $Message" -ForegroundColor Red
        $script:FAILURES++
    }
}

# Check prerequisites
Write-Host "📋 Checking Prerequisites..." -ForegroundColor Blue
Write-Host ""

try {
    $dotnetVersion = dotnet --version
    Print-Status "success" ".NET SDK installed: $dotnetVersion"
} catch {
    Print-Status "failure" ".NET SDK not found (required for backend)"
}

try {
    $nodeVersion = node --version
    Print-Status "success" "Node.js installed: $nodeVersion"
} catch {
    Print-Status "failure" "Node.js not found (required for frontend)"
}

try {
    $npmVersion = npm --version
    Print-Status "success" "npm installed: $npmVersion"
} catch {
    Print-Status "failure" "npm not found (required for frontend)"
}

Write-Host ""

# Check project structure
Write-Host "📁 Checking Project Structure..." -ForegroundColor Blue
Write-Host ""

if (Test-Path "Backend\YurtCord.API") {
    Print-Status "success" "Backend directory exists"
} else {
    Print-Status "failure" "Backend directory not found"
}

if (Test-Path "Backend\YurtCord.API\Program.cs") {
    Print-Status "success" "Backend entry point exists"
} else {
    Print-Status "failure" "Backend Program.cs not found"
}

if (Test-Path "Frontend") {
    Print-Status "success" "Frontend directory exists"
} else {
    Print-Status "failure" "Frontend directory not found"
}

if (Test-Path "Frontend\package.json") {
    Print-Status "success" "Frontend package.json exists"
} else {
    Print-Status "failure" "Frontend package.json not found"
}

if (Test-Path "Frontend\node_modules") {
    Print-Status "success" "Frontend dependencies installed"
} else {
    Print-Status "warning" "Frontend dependencies not installed (run: cd Frontend; npm install)"
}

Write-Host ""

# Check configuration files
Write-Host "⚙️  Checking Configuration..." -ForegroundColor Blue
Write-Host ""

if (Test-Path "Backend\YurtCord.API\appsettings.json") {
    Print-Status "success" "Backend configuration exists"
} else {
    Print-Status "failure" "Backend appsettings.json not found"
}

if (Test-Path "Frontend\.env") {
    Print-Status "success" "Frontend .env file exists"
} else {
    if (Test-Path "Frontend\.env.example") {
        Print-Status "warning" "Frontend .env not found (copy from .env.example)"
    } else {
        Print-Status "warning" "Frontend .env not found"
    }
}

Write-Host ""

# Check if services are running
Write-Host "🔌 Checking Running Services..." -ForegroundColor Blue
Write-Host ""

# Check Backend API
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Print-Status "success" "Backend API is running (http://localhost:5000)"
    }
} catch {
    Print-Status "warning" "Backend API not running (start with: cd Backend\YurtCord.API; dotnet run)"
}

# Check Frontend
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5173" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
    Print-Status "success" "Frontend is running (http://localhost:5173)"
} catch {
    Print-Status "warning" "Frontend not running (start with: cd Frontend; npm run dev)"
}

# Check Swagger
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/swagger/index.html" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Print-Status "success" "Swagger UI accessible (http://localhost:5000/swagger)"
    }
} catch {
    # Ignore if backend is not running
}

Write-Host ""

# Check database
Write-Host "💾 Checking Database..." -ForegroundColor Blue
Write-Host ""

if (Test-Path "Data\yurtcord.db") {
    $dbSize = (Get-Item "Data\yurtcord.db").Length / 1KB
    Print-Status "success" "SQLite database exists ($([math]::Round($dbSize, 2)) KB)"
} else {
    Print-Status "warning" "SQLite database not found (will be created on first run)"
}

Write-Host ""

# Check startup scripts
Write-Host "🚀 Checking Startup Scripts..." -ForegroundColor Blue
Write-Host ""

if (Test-Path "scripts\start.ps1") {
    Print-Status "success" "start.ps1 exists"
} else {
    Print-Status "failure" "start.ps1 not found"
}

if (Test-Path "scripts\stop.ps1") {
    Print-Status "success" "stop.ps1 exists"
} else {
    Print-Status "failure" "stop.ps1 not found"
}

if (Test-Path "scripts\start.bat") {
    Print-Status "success" "start.bat exists"
} else {
    Print-Status "warning" "start.bat not found"
}

Write-Host ""

# Summary
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "📊 Summary" -ForegroundColor Blue
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "✓ Success: $SUCCESS checks passed" -ForegroundColor Green
Write-Host "⚠ Warnings: $WARNINGS warnings" -ForegroundColor Yellow
Write-Host "✗ Failures: $FAILURES failures" -ForegroundColor Red
Write-Host ""

if ($FAILURES -gt 0) {
    Write-Host "⚠️  Some critical checks failed. Please fix the failures above." -ForegroundColor Red
    Write-Host ""
    exit 1
} elseif ($WARNINGS -gt 0) {
    Write-Host "⚠️  Some warnings detected. Review the warnings above." -ForegroundColor Yellow
    Write-Host ""
    exit 0
} else {
    Write-Host "✅ All checks passed! YurtCord is ready to run." -ForegroundColor Green
    Write-Host ""
    Write-Host "🚀 Start YurtCord:"
    Write-Host "   .\scripts\start.ps1"
    Write-Host ""
    Write-Host "📚 Documentation:"
    Write-Host "   GETTING_STARTED.md"
    Write-Host ""
    exit 0
}
