@echo off
REM YurtCord Quick Start Script (Batch)
REM This script starts both Backend and Frontend in embedded mode

echo.
echo ====================================================
echo    YurtCord - Quick Start
echo ====================================================
echo.

REM Check if .NET is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] .NET SDK not found!
    echo Please install .NET 8.0 SDK from:
    echo https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Check if Node.js is installed
where node >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Node.js not found!
    echo Please install Node.js 18+ from:
    echo https://nodejs.org/
    pause
    exit /b 1
)

echo [OK] .NET SDK found
echo [OK] Node.js found
echo.

REM Install Frontend dependencies if needed
if not exist "Frontend\node_modules" (
    echo [INFO] Installing Frontend dependencies...
    cd Frontend
    call npm install
    cd ..
    echo [OK] Frontend dependencies installed
    echo.
)

REM Create .env file if it doesn't exist
if not exist "Frontend\.env" (
    echo [INFO] Creating Frontend .env file...
    copy "Frontend\.env.example" "Frontend\.env" >nul
    echo [OK] Frontend .env created
    echo.
)

echo [INFO] Starting Backend API (Embedded Mode)...
echo        Database: SQLite (auto-created)
echo        API: http://localhost:5000
echo        Swagger: http://localhost:5000/swagger
echo.

REM Start Backend in new window
start "YurtCord Backend" /MIN cmd /c "cd Backend\YurtCord.API && dotnet run"

echo [OK] Backend started
echo.

REM Wait for backend to be ready
echo [INFO] Waiting for Backend to be ready...
timeout /t 5 /nobreak >nul

echo [INFO] Starting Frontend...
echo        Frontend: http://localhost:5173
echo.

REM Start Frontend in new window
start "YurtCord Frontend" cmd /c "cd Frontend && npm run dev"

echo [OK] Frontend started
echo.
echo ====================================================
echo    YurtCord is running!
echo ====================================================
echo.
echo Access Points:
echo   Frontend:  http://localhost:5173
echo   API:       http://localhost:5000
echo   Swagger:   http://localhost:5000/swagger
echo.
echo Test Account:
echo   Email: alice@example.com
echo   Password: Password123!
echo.
echo To stop YurtCord, run: scripts\stop.bat
echo.
echo Press any key to exit this window...
pause >nul
