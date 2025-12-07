# Nginx Management Utility for Windows
# Quick commands to manage Nginx service

param(
    [Parameter(Position=0)]
    [ValidateSet("start", "stop", "restart", "reload", "status", "test", "logs")]
    [string]$Command = "status"
)

$nginxDir = "C:\nginx"
$nginxExe = Join-Path $nginxDir "nginx.exe"

function Show-Status {
    Write-Host "=== Nginx Status ===" -ForegroundColor Cyan
    $processes = Get-Process nginx -ErrorAction SilentlyContinue
    
    if ($processes) {
        Write-Host "✅ Nginx is running" -ForegroundColor Green
        Write-Host ""
        Write-Host "Processes:" -ForegroundColor Yellow
        $processes | Format-Table Id, ProcessName, StartTime, WorkingSet -AutoSize
        
        # Check ports
        Write-Host "Listening Ports:" -ForegroundColor Yellow
        $ports = netstat -ano | Select-String ":80 " | Select-String "LISTENING"
        $ports443 = netstat -ano | Select-String ":443" | Select-String "LISTENING"
        
        if ($ports) { Write-Host "  ✅ Port 80 (HTTP)" -ForegroundColor Green }
        if ($ports443) { Write-Host "  ✅ Port 443 (HTTPS)" -ForegroundColor Green }
        
    } else {
        Write-Host "❌ Nginx is not running" -ForegroundColor Red
    }
}

function Start-NginxService {
    Write-Host "Starting Nginx..." -ForegroundColor Yellow
    
    $processes = Get-Process nginx -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "⚠️  Nginx is already running" -ForegroundColor Yellow
        Show-Status
        return
    }
    
    if (-not (Test-Path $nginxExe)) {
        Write-Host "❌ Nginx not found at: $nginxExe" -ForegroundColor Red
        Write-Host "Please install Nginx to C:\nginx" -ForegroundColor Yellow
        return
    }
    
    Push-Location $nginxDir
    Start-Process -FilePath "nginx.exe" -WindowStyle Hidden
    Pop-Location
    
    Start-Sleep -Seconds 2
    Show-Status
}

function Stop-NginxService {
    Write-Host "Stopping Nginx..." -ForegroundColor Yellow
    
    $processes = Get-Process nginx -ErrorAction SilentlyContinue
    if (-not $processes) {
        Write-Host "ℹ️  Nginx is not running" -ForegroundColor Gray
        return
    }
    
    Push-Location $nginxDir
    & .\nginx.exe -s stop 2>&1 | Out-Null
    Pop-Location
    
    Start-Sleep -Seconds 2
    
    $stillRunning = Get-Process nginx -ErrorAction SilentlyContinue
    if (-not $stillRunning) {
        Write-Host "✅ Nginx stopped successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Forcefully stopping remaining processes..." -ForegroundColor Yellow
        Stop-Process -Name nginx -Force
        Write-Host "✅ Nginx stopped" -ForegroundColor Green
    }
}

function Restart-NginxService {
    Write-Host "Restarting Nginx..." -ForegroundColor Yellow
    Stop-NginxService
    Start-Sleep -Seconds 1
    Start-NginxService
}

function Reload-NginxConfig {
    Write-Host "Reloading Nginx configuration..." -ForegroundColor Yellow
    
    $processes = Get-Process nginx -ErrorAction SilentlyContinue
    if (-not $processes) {
        Write-Host "❌ Nginx is not running" -ForegroundColor Red
        Write-Host "Start Nginx first with: .\manage.ps1 start" -ForegroundColor Yellow
        return
    }
    
    Push-Location $nginxDir
    & .\nginx.exe -s reload 2>&1 | Out-Null
    Pop-Location
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Configuration reloaded successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to reload configuration" -ForegroundColor Red
        Write-Host "Run test first: .\manage.ps1 test" -ForegroundColor Yellow
    }
}

function Test-NginxConfig {
    Write-Host "Testing Nginx configuration..." -ForegroundColor Yellow
    
    if (-not (Test-Path $nginxExe)) {
        Write-Host "❌ Nginx not found at: $nginxExe" -ForegroundColor Red
        return
    }
    
    Push-Location $nginxDir
    $output = & .\nginx.exe -t 2>&1
    Pop-Location
    
    Write-Host $output
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Configuration is valid" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "❌ Configuration has errors" -ForegroundColor Red
    }
}

function Show-Logs {
    Write-Host "=== Nginx Logs ===" -ForegroundColor Cyan
    Write-Host ""
    
    $errorLog = Join-Path $nginxDir "logs\error.log"
    $accessLog = Join-Path $nginxDir "logs\access.log"
    
    if (Test-Path $errorLog) {
        Write-Host "Latest Error Log (last 20 lines):" -ForegroundColor Yellow
        Get-Content $errorLog -Tail 20
        Write-Host ""
    } else {
        Write-Host "No error log found" -ForegroundColor Gray
    }
    
    if (Test-Path $accessLog) {
        Write-Host "Latest Access Log (last 10 lines):" -ForegroundColor Yellow
        Get-Content $accessLog -Tail 10
        Write-Host ""
    } else {
        Write-Host "No access log found" -ForegroundColor Gray
    }
    
    Write-Host "Full logs at:" -ForegroundColor Cyan
    Write-Host "  Error:  $errorLog" -ForegroundColor Gray
    Write-Host "  Access: $accessLog" -ForegroundColor Gray
}

# Execute command
switch ($Command) {
    "start"   { Start-NginxService }
    "stop"    { Stop-NginxService }
    "restart" { Restart-NginxService }
    "reload"  { Reload-NginxConfig }
    "status"  { Show-Status }
    "test"    { Test-NginxConfig }
    "logs"    { Show-Logs }
}

Write-Host ""
Write-Host "Available commands:" -ForegroundColor Cyan
Write-Host "  .\manage.ps1 start   - Start Nginx" -ForegroundColor Gray
Write-Host "  .\manage.ps1 stop    - Stop Nginx" -ForegroundColor Gray
Write-Host "  .\manage.ps1 restart - Restart Nginx" -ForegroundColor Gray
Write-Host "  .\manage.ps1 reload  - Reload config without downtime" -ForegroundColor Gray
Write-Host "  .\manage.ps1 status  - Show status" -ForegroundColor Gray
Write-Host "  .\manage.ps1 test    - Test configuration" -ForegroundColor Gray
Write-Host "  .\manage.ps1 logs    - View recent logs" -ForegroundColor Gray
