# Nginx HTTPS Setup - Complete Checklist

## ✅ Setup Completed

The following files have been created in the `nginx/` directory:

1. **nginx.conf** - Complete Nginx reverse proxy configuration with:
   - SSL/TLS encryption (HTTPS on port 443)
   - HTTP to HTTPS redirect (port 80)
   - Rate limiting (100 req/s API, 10 req/s webhooks)
   - WebSocket support for SignalR
   - Security headers (HSTS, X-Frame-Options, CSP)
   - Proper proxy headers for ASP.NET Core

2. **setup.ps1** - Automated setup script (PowerShell)
3. **setup.bat** - Quick launcher for setup.ps1
4. **manage.ps1** - Management utility for daily operations
5. **README.md** - Complete documentation
6. **QUICKSTART.md** - Quick reference guide

## 🚀 Quick Start (3 Steps)

### Step 1: Install Nginx
Download and extract to `C:\nginx`: https://nginx.org/en/download.html

### Step 2: Run Setup Script
```powershell
# Right-click and "Run as Administrator"
nginx\setup.bat

# Or in PowerShell:
cd nginx
.\setup.ps1
```

### Step 3: Start API and Test
```powershell
# Terminal 1: Start API
cd SubscriptionSystem
dotnet run

# Terminal 2: Test HTTPS endpoints
curl https://localhost/health -k
Start-Process https://localhost/swagger
```

## 📋 Manual Setup (If Preferred)

### 1. Install Nginx
```powershell
# Download Windows version
# Extract to C:\nginx
```

### 2. Copy Configuration
```powershell
Copy-Item nginx\nginx.conf -Destination C:\nginx\conf\nginx.conf -Force
```

### 3. Generate SSL Certificate
```powershell
# Option A: Using OpenSSL
cd nginx\ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 `
  -keyout idansure.key `
  -out idansure.crt `
  -subj "/C=NG/ST=Lagos/L=Lagos/O=IdanSure/CN=api.idansure.com"

# Option B: For production, use Let's Encrypt
certbot certonly --webroot -w C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\nginx\ssl `
  -d api.idansure.com
```

### 4. Configure Firewall
```powershell
# Run as Administrator
New-NetFirewallRule -DisplayName "Nginx HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "Nginx HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

### 5. Test and Start
```powershell
cd C:\nginx
.\nginx.exe -t        # Test configuration
start nginx           # Start service
```

## 🔧 Daily Management

### Using the Management Script
```powershell
cd nginx

.\manage.ps1 status   # Check if running
.\manage.ps1 start    # Start Nginx
.\manage.ps1 stop     # Stop Nginx
.\manage.ps1 restart  # Restart Nginx
.\manage.ps1 reload   # Reload config (no downtime)
.\manage.ps1 test     # Test configuration
.\manage.ps1 logs     # View recent logs
```

### Direct Commands
```powershell
# Start
cd C:\nginx
start nginx

# Stop
nginx -s stop

# Reload config
nginx -s reload

# Check status
Get-Process nginx

# View logs
Get-Content C:\nginx\logs\error.log -Tail 50
Get-Content C:\nginx\logs\access.log -Tail 50
```

## 🧪 Testing Guide

### 1. Verify Nginx is Running
```powershell
Get-Process nginx
netstat -ano | findstr ":80"
netstat -ano | findstr ":443"
```

### 2. Test Backend Connection
```powershell
# Check API is running
Test-NetConnection -ComputerName localhost -Port 5279
curl http://localhost:5279/health
```

### 3. Test HTTPS Endpoints
```powershell
# Health check
curl https://localhost/health -k

# Swagger UI
Start-Process https://localhost/swagger

# API endpoints
curl https://localhost/api/predictionpost/today -k

# Test redirect (should redirect to HTTPS)
curl -L http://localhost/health
```

### 4. Test from Remote Machine
```powershell
# Replace with your server IP or domain
curl https://api.idansure.com/health
curl https://api.idansure.com/api/predictionpost/today
```

## 🏗️ Architecture

```
┌─────────────────┐
│   Client App    │
│  (Browser/API)  │
└────────┬────────┘
         │ HTTPS (443)
         ▼
┌─────────────────┐
│  Nginx Proxy    │  ← SSL/TLS Termination
│   :80 → :443    │  ← Rate Limiting
│                 │  ← Security Headers
└────────┬────────┘
         │ HTTP (5279)
         ▼
┌─────────────────┐
│  ASP.NET Core   │
│      API        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   PostgreSQL    │
│    Database     │
└─────────────────┘
```

## 🔒 Security Features

✅ **SSL/TLS Encryption** (TLS 1.2/1.3)
✅ **HTTP to HTTPS Redirect** (All HTTP traffic redirected)
✅ **Security Headers** (HSTS, X-Frame-Options, CSP, etc.)
✅ **Rate Limiting** (100 req/s API, 10 req/s webhooks)
✅ **Strong Ciphers** (Modern cipher suites only)
✅ **WebSocket Support** (For SignalR)
✅ **DDoS Protection** (Connection limits, timeouts)
✅ **Request Buffering** (Upload size limits)

## 📊 Monitoring

### Key Metrics to Monitor
```powershell
# Request rate
Get-Content C:\nginx\logs\access.log | Measure-Object -Line

# Error rate
Get-Content C:\nginx\logs\error.log -Tail 100

# Response times (requires access log analysis)
# Process count
(Get-Process nginx).Count

# Memory usage
Get-Process nginx | Select-Object Name, WorkingSet64

# Port status
netstat -ano | findstr ":443" | findstr "ESTABLISHED"
```

## 🚨 Troubleshooting

### Issue: Port 80/443 Already in Use
```powershell
# Find process
netstat -ano | findstr ":80"
netstat -ano | findstr ":443"

# Stop IIS if needed
iisreset /stop

# Or kill specific process
Stop-Process -Id <PID> -Force
```

### Issue: 502 Bad Gateway
```powershell
# Check if API is running
Test-NetConnection -ComputerName localhost -Port 5279

# Restart API
cd SubscriptionSystem
dotnet run

# Check Nginx error log
Get-Content C:\nginx\logs\error.log -Tail 50
```

### Issue: Certificate Errors
- Self-signed certificates will show browser warnings
- This is normal for development
- For production, use Let's Encrypt or commercial certificate

### Issue: Configuration Errors
```powershell
# Test configuration
cd C:\nginx
.\nginx.exe -t

# Check syntax errors in nginx.conf
# Verify SSL certificate paths exist
Test-Path C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\nginx\ssl\idansure.crt
Test-Path C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\nginx\ssl\idansure.key
```

## 🌐 Production Deployment

### 1. Get Production SSL Certificate
```powershell
# Using Let's Encrypt (recommended)
certbot certonly --webroot -w C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\nginx\ssl `
  -d api.idansure.com

# Update nginx.conf
ssl_certificate     C:/Certbot/live/api.idansure.com/fullchain.pem;
ssl_certificate_key C:/Certbot/live/api.idansure.com/privkey.pem;
```

### 2. Configure DNS
```
A Record: api.idansure.com → [Server IP Address]
```

### 3. Set Up as Windows Service
```powershell
# Download NSSM: https://nssm.cc/download
nssm install nginx C:\nginx\nginx.exe
nssm set nginx AppDirectory C:\nginx
nssm start nginx

# Verify service
Get-Service nginx
```

### 4. Configure Auto-Renewal (Let's Encrypt)
```powershell
# Create scheduled task for certificate renewal
$action = New-ScheduledTaskAction -Execute "certbot" -Argument "renew --post-hook 'nginx -s reload'"
$trigger = New-ScheduledTaskTrigger -Daily -At 3am
Register-ScheduledTask -TaskName "RenewSSL" -Action $action -Trigger $trigger -RunLevel Highest
```

## 📝 Configuration Files Reference

### nginx.conf Sections
- **upstream**: Backend server pool (ASP.NET Core on :5279)
- **server (80)**: HTTP server with redirect
- **server (443)**: HTTPS server with SSL and proxy rules
- **location blocks**: Route-specific configurations

### Key Paths
- Config: `C:\nginx\conf\nginx.conf`
- SSL Certs: `nginx\ssl\idansure.crt` and `idansure.key`
- Error Log: `C:\nginx\logs\error.log`
- Access Log: `C:\nginx\logs\access.log`
- PID File: `C:\nginx\logs\nginx.pid`

## ✅ Production Readiness Checklist

- [ ] Nginx installed and configured
- [ ] Valid SSL certificate installed (not self-signed)
- [ ] DNS A record pointing to server
- [ ] Firewall rules configured
- [ ] Nginx running as Windows service
- [ ] Log rotation configured
- [ ] Monitoring alerts set up
- [ ] SSL auto-renewal configured
- [ ] Backup strategy documented
- [ ] Load testing completed
- [ ] Security headers verified
- [ ] Rate limits tuned for production traffic

## 📚 Additional Resources

- Nginx Documentation: https://nginx.org/en/docs/
- ASP.NET Core Proxy: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer
- Let's Encrypt: https://letsencrypt.org/
- NSSM (Windows Service): https://nssm.cc/

## 🆘 Support

For issues or questions:
1. Check logs: `Get-Content C:\nginx\logs\error.log -Tail 50`
2. Test config: `nginx -t`
3. Verify backend: `curl http://localhost:5279/health`
4. Review documentation in `README.md`

---

**Setup Created:** December 1, 2025
**For:** IdanSure API (https://api.idansure.com)
**Backend:** ASP.NET Core 8 on Windows
