# IdanSure API - Nginx Setup Guide

## Overview
This Nginx configuration provides:
- HTTPS encryption with SSL/TLS
- Reverse proxy to ASP.NET Core API (port 5279)
- Rate limiting for API and webhook endpoints
- WebSocket support for SignalR
- Security headers
- Automatic HTTP to HTTPS redirect

## Prerequisites

### 1. Install Nginx for Windows
Download from: https://nginx.org/en/download.html

```powershell
# Extract to C:\nginx or your preferred location
# Example: C:\nginx\
```

### 2. Copy Configuration
```powershell
# Copy the nginx.conf to your Nginx installation
Copy-Item .\nginx\nginx.conf -Destination C:\nginx\conf\nginx.conf -Force
```

## SSL Certificate Setup

### Option 1: Self-Signed Certificate (Development/Testing)

Generate a self-signed certificate for testing:

```powershell
# Navigate to nginx\ssl directory
cd C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\nginx\ssl

# Generate private key and certificate (requires OpenSSL)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 `
  -keyout idansure.key `
  -out idansure.crt `
  -subj "/C=NG/ST=Lagos/L=Lagos/O=IdanSure/CN=api.idansure.com"
```

If OpenSSL is not installed on Windows, download from: https://slproweb.com/products/Win32OpenSSL.html

### Option 2: Let's Encrypt (Production)

For production, use Certbot for Windows:

1. Download Certbot: https://certbot.eff.org/instructions?ws=nginx&os=windows
2. Run:
```powershell
certbot certonly --webroot -w C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\nginx\ssl `
  -d api.idansure.com
```

3. Update nginx.conf paths:
```nginx
ssl_certificate     C:/Certbot/live/api.idansure.com/fullchain.pem;
ssl_certificate_key C:/Certbot/live/api.idansure.com/privkey.pem;
```

### Option 3: Use AWS/Cloud Provider Certificate

If running on AWS, use AWS Certificate Manager and terminate SSL at ALB/CloudFront level.

## Configuration Steps

### 1. Update Nginx Configuration

Edit `C:\nginx\conf\nginx.conf` and verify:
- Backend server port matches your API (default: 5279)
- SSL certificate paths are correct
- Server name matches your domain

### 2. Test Configuration

```powershell
# Test Nginx configuration
C:\nginx\nginx.exe -t

# Expected output:
# nginx: the configuration file C:\nginx/conf/nginx.conf syntax is ok
# nginx: configuration file C:\nginx/conf/nginx.conf test is successful
```

### 3. Start Nginx

```powershell
# Start Nginx
cd C:\nginx
start nginx

# Or run in current window:
.\nginx.exe
```

### 4. Verify Nginx is Running

```powershell
# Check processes
Get-Process nginx

# Check if port 80 and 443 are listening
netstat -ano | findstr ":80"
netstat -ano | findstr ":443"
```

## Managing Nginx on Windows

### Start Nginx
```powershell
cd C:\nginx
start nginx
```

### Stop Nginx
```powershell
nginx -s stop
```

### Reload Configuration (without downtime)
```powershell
nginx -s reload
```

### Restart Nginx
```powershell
nginx -s quit
start nginx
```

### Check Nginx Status
```powershell
Get-Process nginx
```

## Testing the Setup

### 1. Start ASP.NET Core API
```powershell
cd C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\SubscriptionSystem
dotnet run
```

The API should start on http://localhost:5279

### 2. Test Endpoints

```powershell
# HTTP (should redirect to HTTPS)
curl http://localhost/health

# HTTPS
curl https://localhost/health -k

# Or in browser:
# https://localhost/swagger
# https://api.idansure.com/swagger (if DNS configured)
```

### 3. Test from External Client

```powershell
# Using Invoke-WebRequest
Invoke-WebRequest -Uri https://api.idansure.com/health -SkipCertificateCheck

# Using curl
curl https://api.idansure.com/api/predictionpost/today
```

## Firewall Configuration

Ensure Windows Firewall allows traffic:

```powershell
# Allow HTTP (port 80)
New-NetFirewallRule -DisplayName "Nginx HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow

# Allow HTTPS (port 443)
New-NetFirewallRule -DisplayName "Nginx HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

## DNS Configuration

For production, point your domain to the server:

```
A Record: api.idansure.com -> [Your Server IP]
```

## Troubleshooting

### Check Nginx Logs
```powershell
# Error log
Get-Content C:\nginx\logs\error.log -Tail 50

# Access log
Get-Content C:\nginx\logs\access.log -Tail 50
```

### Common Issues

**1. Port 80/443 already in use**
```powershell
# Find what's using the port
netstat -ano | findstr ":80"
netstat -ano | findstr ":443"

# Stop IIS if running
iisreset /stop
```

**2. Certificate errors**
- Verify certificate files exist in `nginx\ssl\` directory
- Check file permissions
- Ensure paths in nginx.conf are correct (use forward slashes)

**3. Backend not reachable**
```powershell
# Verify API is running
Test-NetConnection -ComputerName localhost -Port 5279

# Check if API responds
curl http://localhost:5279/health
```

**4. 502 Bad Gateway**
- API is not running on port 5279
- Firewall blocking localhost communication
- Check upstream configuration in nginx.conf

## Windows Service Setup (Optional)

To run Nginx as a Windows service, use NSSM (Non-Sucking Service Manager):

1. Download NSSM: https://nssm.cc/download
2. Install service:

```powershell
nssm install nginx "C:\nginx\nginx.exe"
nssm set nginx AppDirectory "C:\nginx"
nssm start nginx
```

## Performance Tuning

For production, adjust in nginx.conf:
```nginx
worker_processes 4;  # Match CPU cores
worker_connections 2048;  # Increase for high traffic
```

## Security Checklist

- ✅ HTTPS enabled with valid certificate
- ✅ HTTP redirects to HTTPS
- ✅ Security headers configured
- ✅ Rate limiting enabled
- ✅ Strong SSL ciphers configured
- ✅ HSTS header enabled
- ⚠️ Update SSL certificates before expiry
- ⚠️ Monitor rate limit logs
- ⚠️ Keep Nginx updated

## Production Deployment Checklist

1. ✅ Install production SSL certificate
2. ✅ Update server_name to actual domain
3. ✅ Configure DNS A record
4. ✅ Set up Nginx as Windows service
5. ✅ Configure log rotation
6. ✅ Set up monitoring/alerts
7. ✅ Test failover scenarios
8. ✅ Configure backup strategy
9. ✅ Document runbooks
10. ✅ Set up SSL certificate auto-renewal

## Monitoring

Monitor these metrics:
- Request rate and response times
- SSL certificate expiry
- Error rate (4xx, 5xx responses)
- Rate limit hits
- Backend health

## Support

For issues:
- Check Nginx docs: https://nginx.org/en/docs/
- ASP.NET Core proxy docs: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer
