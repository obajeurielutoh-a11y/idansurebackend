@echo off
REM Quick Nginx Setup - Run as Administrator
REM This batch file launches the PowerShell setup script

echo ========================================
echo   IdanSure API - Nginx HTTPS Setup
echo ========================================
echo.

REM Check for administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo.
    echo Right-click this file and select "Run as Administrator"
    echo.
    pause
    exit /b 1
)

REM Run the PowerShell setup script
powershell -ExecutionPolicy Bypass -File "%~dp0setup.ps1"

pause
