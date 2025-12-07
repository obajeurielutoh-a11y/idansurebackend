# Quick Nginx Setup Script for Windows
# Run this script with elevated privileges (Run as Administrator)

Write-Host "=== IdanSure API - Nginx HTTPS Setup ===" -ForegroundColor Cyan
Write-Host ""

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$nginxDir = "C:\nginx"

# Check if running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "⚠️  Please run this script as Administrator!" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit
}

# Step 1: Check if Nginx is installed
Write-Host "Step 1: Checking Nginx installation..." -ForegroundColor Yellow
if (-not (Test-Path $nginxDir)) {
    Write-Host "❌ Nginx not found at $nginxDir" -ForegroundColor Red
    Write-Host "Please download and extract Nginx to C:\nginx" -ForegroundColor Yellow
    Write-Host "Download from: https://nginx.org/en/download.html" -ForegroundColor Cyan
    Write-Host ""
    $download = Read-Host "Would you like to open the download page? (Y/N)"
    if ($download -eq "Y" -or $download -eq "y") {
        Start-Process "https://nginx.org/en/download.html"
    }
    Read-Host "Press Enter to exit"
    exit
} else {
    Write-Host "✅ Nginx found at $nginxDir" -ForegroundColor Green
}

# Step 2: Stop existing Nginx processes
Write-Host ""
Write-Host "Step 2: Stopping existing Nginx processes..." -ForegroundColor Yellow
$nginxProcesses = Get-Process nginx -ErrorAction SilentlyContinue
if ($nginxProcesses) {
    Stop-Process -Name nginx -Force
    Start-Sleep -Seconds 2
    Write-Host "✅ Stopped existing Nginx processes" -ForegroundColor Green
} else {
    Write-Host "ℹ️  No Nginx processes running" -ForegroundColor Gray
}

# Step 3: Copy configuration
Write-Host ""
Write-Host "Step 3: Copying Nginx configuration..." -ForegroundColor Yellow
$sourceConf = Join-Path $scriptDir "nginx.conf"
$destConf = Join-Path $nginxDir "conf\nginx.conf"

if (Test-Path $sourceConf) {
    # Backup existing config
    if (Test-Path $destConf) {
        $backup = "$destConf.backup.$(Get-Date -Format 'yyyyMMddHHmmss')"
        Copy-Item $destConf $backup
        Write-Host "📦 Backed up existing config to: $backup" -ForegroundColor Cyan
    }
    
    Copy-Item $sourceConf $destConf -Force
    Write-Host "✅ Configuration copied successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Configuration file not found at: $sourceConf" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit
}

# Step 4: Create SSL directory
Write-Host ""
Write-Host "Step 4: Setting up SSL directory..." -ForegroundColor Yellow
$sslDir = Join-Path $scriptDir "ssl"
if (-not (Test-Path $sslDir)) {
    New-Item -ItemType Directory -Path $sslDir | Out-Null
}
Write-Host "✅ SSL directory ready at: $sslDir" -ForegroundColor Green

# Step 5: Generate self-signed certificate
Write-Host ""
Write-Host "Step 5: Generating self-signed SSL certificate..." -ForegroundColor Yellow

$certPath = Join-Path $sslDir "idansure.crt"
$keyPath = Join-Path $sslDir "idansure.key"

# Check if OpenSSL is available
$opensslCmd = Get-Command openssl -ErrorAction SilentlyContinue

if ($opensslCmd) {
    # Generate certificate using OpenSSL
    $opensslArgs = @(
        "req", "-x509", "-nodes", "-days", "365",
        "-newkey", "rsa:2048",
        "-keyout", $keyPath,
        "-out", $certPath,
        "-subj", "/C=NG/ST=Lagos/L=Lagos/O=IdanSure/CN=api.idansure.com"
    )
    
    & openssl $opensslArgs 2>&1 | Out-Null
    
    if (Test-Path $certPath -and Test-Path $keyPath) {
        Write-Host "✅ Self-signed certificate generated successfully" -ForegroundColor Green
        Write-Host "   Certificate: $certPath" -ForegroundColor Gray
        Write-Host "   Key: $keyPath" -ForegroundColor Gray
    } else {
        Write-Host "❌ Failed to generate certificate" -ForegroundColor Red
    }
} else {
    # Use PowerShell to create certificate (Windows 10+)
    Write-Host "OpenSSL not found, using PowerShell certificate generation..." -ForegroundColor Yellow
    
    try {
        $cert = New-SelfSignedCertificate `
            -DnsName "api.idansure.com", "localhost" `
            -CertStoreLocation "Cert:\LocalMachine\My" `
            -KeyExportPolicy Exportable `
            -KeySpec Signature `
            -KeyLength 2048 `
            -KeyAlgorithm RSA `
            -HashAlgorithm SHA256 `
            -NotAfter (Get-Date).AddYears(1)
        
        # Export certificate
        $certPassword = ConvertTo-SecureString -String "idansure123" -Force -AsPlainText
        Export-PfxCertificate -Cert $cert -FilePath (Join-Path $sslDir "idansure.pfx") -Password $certPassword | Out-Null
        
        # Convert to PEM format (Nginx requires PEM)
        # Note: This requires OpenSSL for PEM conversion
        Write-Host "⚠️  Certificate created in Windows cert store" -ForegroundColor Yellow
        Write-Host "⚠️  For Nginx, you need OpenSSL to convert to PEM format" -ForegroundColor Yellow
        Write-Host "   Install OpenSSL from: https://slproweb.com/products/Win32OpenSSL.html" -ForegroundColor Cyan
        
    } catch {
        Write-Host "❌ Failed to generate certificate: $_" -ForegroundColor Red
    }
}

# Step 6: Configure firewall
Write-Host ""
Write-Host "Step 6: Configuring Windows Firewall..." -ForegroundColor Yellow

try {
    # Check if rules already exist
    $httpRule = Get-NetFirewallRule -DisplayName "Nginx HTTP" -ErrorAction SilentlyContinue
    $httpsRule = Get-NetFirewallRule -DisplayName "Nginx HTTPS" -ErrorAction SilentlyContinue
    
    if (-not $httpRule) {
        New-NetFirewallRule -DisplayName "Nginx HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow | Out-Null
        Write-Host "✅ Added firewall rule for HTTP (port 80)" -ForegroundColor Green
    } else {
        Write-Host "ℹ️  HTTP firewall rule already exists" -ForegroundColor Gray
    }
    
    if (-not $httpsRule) {
        New-NetFirewallRule -DisplayName "Nginx HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow | Out-Null
        Write-Host "✅ Added firewall rule for HTTPS (port 443)" -ForegroundColor Green
    } else {
        Write-Host "ℹ️  HTTPS firewall rule already exists" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️  Could not configure firewall: $_" -ForegroundColor Yellow
    Write-Host "   You may need to manually allow ports 80 and 443" -ForegroundColor Yellow
}

# Step 7: Test configuration
Write-Host ""
Write-Host "Step 7: Testing Nginx configuration..." -ForegroundColor Yellow
Push-Location $nginxDir
$testResult = & .\nginx.exe -t 2>&1
Pop-Location

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Nginx configuration is valid" -ForegroundColor Green
} else {
    Write-Host "❌ Nginx configuration has errors:" -ForegroundColor Red
    Write-Host $testResult -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit
}

# Step 8: Start Nginx
Write-Host ""
Write-Host "Step 8: Starting Nginx..." -ForegroundColor Yellow

Push-Location $nginxDir
Start-Process -FilePath "nginx.exe" -WindowStyle Hidden
Pop-Location

Start-Sleep -Seconds 2

$nginxProcess = Get-Process nginx -ErrorAction SilentlyContinue
if ($nginxProcess) {
    Write-Host "✅ Nginx started successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "=== Setup Complete! ===" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📋 Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Start your ASP.NET Core API on port 5279" -ForegroundColor White
    Write-Host "   cd $projectRoot\SubscriptionSystem" -ForegroundColor Gray
    Write-Host "   dotnet run" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Access your API via HTTPS:" -ForegroundColor White
    Write-Host "   https://localhost/swagger" -ForegroundColor Cyan
    Write-Host "   https://api.idansure.com/swagger (if DNS configured)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "3. Test endpoints:" -ForegroundColor White
    Write-Host "   curl https://localhost/health -k" -ForegroundColor Gray
    Write-Host "   curl https://localhost/api/predictionpost/today -k" -ForegroundColor Gray
    Write-Host ""
    Write-Host "📝 Management Commands:" -ForegroundColor Yellow
    Write-Host "   Stop:   nginx -s stop" -ForegroundColor Gray
    Write-Host "   Reload: nginx -s reload" -ForegroundColor Gray
    Write-Host "   Logs:   Get-Content C:\nginx\logs\error.log -Tail 50" -ForegroundColor Gray
    Write-Host ""
    Write-Host "⚠️  Note: Self-signed certificate will show security warnings" -ForegroundColor Yellow
    Write-Host "   For production, use a proper SSL certificate (Let's Encrypt)" -ForegroundColor Yellow
} else {
    Write-Host "❌ Failed to start Nginx" -ForegroundColor Red
    Write-Host "Check error log: Get-Content C:\nginx\logs\error.log -Tail 50" -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Press Enter to exit"
