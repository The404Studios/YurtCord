@echo off
REM YurtCord Stop Script (Batch)
REM This script stops both Backend and Frontend services

echo.
echo ====================================================
echo    YurtCord - Stop Services
echo ====================================================
echo.

REM Kill processes on port 5000 (Backend)
echo [INFO] Stopping Backend (port 5000)...
for /f "tokens=5" %%a in ('netstat -aon ^| find ":5000" ^| find "LISTENING"') do (
    taskkill /F /PID %%a >nul 2>nul
    if %ERRORLEVEL% EQU 0 (
        echo [OK] Backend stopped
    )
)

REM Kill processes on port 5173 (Frontend)
echo [INFO] Stopping Frontend (port 5173)...
for /f "tokens=5" %%a in ('netstat -aon ^| find ":5173" ^| find "LISTENING"') do (
    taskkill /F /PID %%a >nul 2>nul
    if %ERRORLEVEL% EQU 0 (
        echo [OK] Frontend stopped
    )
)

REM Also kill by window title
taskkill /FI "WINDOWTITLE eq YurtCord Backend*" /F >nul 2>nul
taskkill /FI "WINDOWTITLE eq YurtCord Frontend*" /F >nul 2>nul

echo.
echo ====================================================
echo    YurtCord stopped!
echo ====================================================
echo.
pause
