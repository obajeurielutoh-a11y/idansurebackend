# Quick Start Guide for Nginx HTTPS Setup

## Prerequisites
- Nginx for Windows: Download from https://nginx.org/en/download.html
- Extract to `C:\nginx`

## Automated Setup (Recommended)

### Run the setup script as Administrator:

```powershell
cd C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\nginx
.\setup.ps1
```

This script will:
1. ✅ Check Nginx installation
2. ✅ Copy configuration files
3. ✅ Generate self-signed SSL certificate
4. ✅ Configure Windows Firewall
5. ✅ Test and start Nginx

## Manual Setup

### 1. Download and Install Nginx
```powershell
# Download from: https://nginx.org/en/download.html
# Extract to C:\nginx
```

### 2. Copy Configuration
```powershell
Copy-Item .\nginx.conf -Destination C:\nginx\conf\nginx.conf -Force
```

### 3. Generate Self-Signed Certificate (requires OpenSSL)
```powershell
cd ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 `
  -keyout idansure.key `
  -out idansure.crt `
  -subj "/C=NG/ST=Lagos/L=Lagos/O=IdanSure/CN=api.idansure.com"
```

### 4. Configure Firewall
```powershell
# Run as Administrator
New-NetFirewallRule -DisplayName "Nginx HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "Nginx HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

### 5. Test Configuration
```powershell
cd C:\nginx
.\nginx.exe -t
```

### 6. Start Nginx
```powershell
cd C:\nginx
start nginx
```

## Verify Setup

### Check Nginx is Running
```powershell
Get-Process nginx
```

### Test Endpoints
```powershell
# HTTP (redirects to HTTPS)
curl http://localhost

# HTTPS
curl https://localhost/health -k

# Swagger UI
Start-Process https://localhost/swagger
```

## Management Commands

### Stop Nginx
```powershell
cd C:\nginx
.\nginx.exe -s stop
```

### Reload Configuration
```powershell
.\nginx.exe -s reload
```

### View Logs
```powershell
Get-Content C:\nginx\logs\error.log -Tail 50
Get-Content C:\nginx\logs\access.log -Tail 50
```

## Testing Full Stack

### 1. Start API
```powershell
cd C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\SubscriptionSystem
dotnet run
```

### 2. Access via HTTPS
- Swagger UI: https://localhost/swagger
- Health Check: https://localhost/health
- Predictions: https://localhost/api/predictionpost/today

## Troubleshooting

### Port Already in Use
```powershell
# Find process using port 80 or 443
netstat -ano | findstr ":80"
netstat -ano | findstr ":443"

# Stop IIS if running
iisreset /stop
```

### Certificate Errors
- Browser will show warning for self-signed cert (this is normal)
- Click "Advanced" → "Proceed to localhost"
- For production, use Let's Encrypt or commercial certificate

### 502 Bad Gateway
- Ensure API is running on http://localhost:5279
- Check: `curl http://localhost:5279/health`

## Production Deployment

For production with proper SSL:

1. Get SSL certificate from Let's Encrypt:
   ```powershell
   certbot certonly --webroot -w C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\nginx\ssl -d api.idansure.com
   ```

2. Update nginx.conf:
   ```nginx
   ssl_certificate     C:/Certbot/live/api.idansure.com/fullchain.pem;
   ssl_certificate_key C:/Certbot/live/api.idansure.com/privkey.pem;
   ```

3. Set up as Windows Service (using NSSM):
   ```powershell
   nssm install nginx C:\nginx\nginx.exe
   nssm start nginx
   ```

## Architecture

```
Client Request (HTTPS)
    ↓
Nginx :443 (SSL Termination)
    ↓
ASP.NET Core API :5279 (HTTP)
    ↓
PostgreSQL Database
```

## Security Features Enabled

✅ HTTPS encryption (TLS 1.2/1.3)
✅ HTTP → HTTPS redirect
✅ Security headers (HSTS, X-Frame-Options, etc.)
✅ Rate limiting (100 req/s for API, 10 req/s for webhooks)
✅ WebSocket support for SignalR
✅ Strong SSL ciphers

## Support

See full documentation in `README.md`
