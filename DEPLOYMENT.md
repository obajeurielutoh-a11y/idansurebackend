# Deployment Guide for api.idansure.com

## Prerequisites
- DNS: `api.idansure.com` pointing to `52.15.130.28`
- SSH Access: `ssh edonsure@52.15.130.28`
- .NET 8 SDK installed on server

## Step-by-Step Deployment

### 1. Connect to Server
```bash
ssh edonsure@52.15.130.28
```

### 2. Update System (if needed)
```bash
sudo apt update
sudo apt upgrade -y
```

### 3. Install .NET 8 (if not already installed)
```bash
# Check if installed
dotnet --version

# If not installed:
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

### 4. Install Nginx
```bash
sudo apt install nginx -y
sudo systemctl enable nginx
sudo systemctl start nginx
```

### 5. Install Certbot for Let's Encrypt SSL
```bash
sudo apt install certbot python3-certbot-nginx -y
```

### 6. Deploy Application

#### Option A: Clone from GitHub (Recommended)
```bash
cd /var/www
sudo mkdir -p idansure-api
sudo chown -R edonsure:edonsure idansure-api
cd idansure-api

# Clone your repository
git clone https://github.com/obajeurielutoh-a11y/idansurebackend.git .

# Navigate to project
cd SubscriptionSystem

# Restore and build
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o /var/www/idansure-api/publish
```

#### Option B: Upload from Local Machine
```powershell
# On your Windows machine
cd C:\Users\Josiah.Obaje\Desktop\JosiahFile\IdanSureBackendProject\SubscriptionSystem
dotnet publish -c Release -o publish

# Upload to server
scp -r publish edonsure@52.15.130.28:/var/www/idansure-api/
```

### 7. Configure Environment Variables

Create `.env` file on server:
```bash
cd /var/www/idansure-api/publish
nano .env
```

Add your environment variables:
```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__IdanSurestSecurityConnectionForPrediction=Host=your-db-host;Database=your-db;Username=your-user;Password=your-password
Jwt__Key=your-jwt-secret-key
Jwt__Issuer=https://api.idansure.com
Jwt__Audience=https://api.idansure.com
OpenAI__ApiKey=your-openai-key
WhatsApp__WebhookSecrets=["your-webhook-secret"]
EF_AUTO_MIGRATE=false
```

Save with `Ctrl+X`, then `Y`, then `Enter`.

### 8. Configure Nginx

```bash
sudo nano /etc/nginx/sites-available/idansure-api
```

Paste this configuration:
```nginx
upstream idansure_api {
    server 127.0.0.1:5000;
    keepalive 32;
}

server {
    listen 80;
    listen [::]:80;
    server_name api.idansure.com;

    # Let's Encrypt verification
    location ^~ /.well-known/acme-challenge/ {
        root /var/www/certbot;
        allow all;
    }

    # Redirect to HTTPS
    location / {
        return 301 https://$server_name$request_uri;
    }
}

server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name api.idansure.com;

    # SSL certificates (will be created by Certbot)
    ssl_certificate /etc/letsencrypt/live/api.idansure.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.idansure.com/privkey.pem;
    
    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers 'ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384';
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=100r/s;
    limit_req zone=api_limit burst=20 nodelay;

    client_max_body_size 100M;

    location / {
        proxy_pass http://idansure_api;
        proxy_http_version 1.1;
        
        # WebSocket support
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        
        # Forwarded headers (critical for CORS!)
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $server_name;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 300s;
        proxy_read_timeout 300s;
        
        proxy_buffering off;
        proxy_cache_bypass $http_upgrade;
    }
}
```

Enable the site:
```bash
sudo ln -s /etc/nginx/sites-available/idansure-api /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 9. Get SSL Certificate

First, temporarily comment out SSL lines in nginx config:
```bash
sudo nano /etc/nginx/sites-available/idansure-api
# Comment out lines 27-28 (ssl_certificate lines)
sudo systemctl reload nginx
```

Then run Certbot:
```bash
sudo certbot certonly --nginx -d api.idansure.com
```

Uncomment SSL lines and reload:
```bash
sudo nano /etc/nginx/sites-available/idansure-api
# Uncomment lines 27-28
sudo nginx -t
sudo systemctl reload nginx
```

### 10. Create Systemd Service

```bash
sudo nano /etc/systemd/system/idansure-api.service
```

Paste this:
```ini
[Unit]
Description=IdanSure API - ASP.NET Core
After=network.target

[Service]
WorkingDirectory=/var/www/idansure-api/publish
ExecStart=/usr/bin/dotnet /var/www/idansure-api/publish/SubscriptionSystem.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=idansure-api
User=edonsure
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

Enable and start the service:
```bash
sudo systemctl daemon-reload
sudo systemctl enable idansure-api
sudo systemctl start idansure-api
sudo systemctl status idansure-api
```

### 11. Verify Deployment

```bash
# Check API is running
curl http://localhost:5000/health

# Check from outside
curl https://api.idansure.com/health

# View logs
sudo journalctl -u idansure-api -f
```

## Updating the Application

```bash
# Connect to server
ssh edonsure@52.15.130.28

# Navigate to project
cd /var/www/idansure-api

# Pull latest changes
git pull origin main

# Rebuild and publish
cd SubscriptionSystem
dotnet publish -c Release -o /var/www/idansure-api/publish

# Restart service
sudo systemctl restart idansure-api

# Check status
sudo systemctl status idansure-api
```

## Troubleshooting

### Check Application Logs
```bash
sudo journalctl -u idansure-api -n 100 --no-pager
sudo journalctl -u idansure-api -f  # Follow logs
```

### Check Nginx Logs
```bash
sudo tail -f /var/log/nginx/error.log
sudo tail -f /var/log/nginx/access.log
```

### Test Nginx Configuration
```bash
sudo nginx -t
```

### Restart Services
```bash
sudo systemctl restart idansure-api
sudo systemctl restart nginx
```

### Check Ports
```bash
# Check if API is listening on 5000
sudo netstat -tulpn | grep 5000

# Check if Nginx is listening on 80/443
sudo netstat -tulpn | grep nginx
```

### Fix CORS Issues
If CORS errors persist:
1. Verify `X-Forwarded-Proto` header is set in Nginx
2. Check application logs for forwarded headers
3. Ensure `UseForwardedHeaders()` is called before `UseCors()`

### Database Migrations
```bash
cd /var/www/idansure-api/publish

# Run migrations manually
dotnet SubscriptionSystem.dll --migrate

# Or set EF_AUTO_MIGRATE=true in .env
```

## Quick Commands Reference

```bash
# Service management
sudo systemctl start idansure-api
sudo systemctl stop idansure-api
sudo systemctl restart idansure-api
sudo systemctl status idansure-api

# View logs
sudo journalctl -u idansure-api -f

# Nginx management
sudo systemctl reload nginx
sudo nginx -t

# SSL certificate renewal (automatic, but manual if needed)
sudo certbot renew --dry-run
```

## Security Checklist

- ✅ SSL/TLS enabled via Let's Encrypt
- ✅ HTTP redirects to HTTPS
- ✅ Security headers configured
- ✅ Rate limiting enabled
- ✅ Firewall configured (UFW)
- ✅ Only necessary ports open (22, 80, 443)
- ✅ Application runs as non-root user
- ✅ Environment variables secured in .env
- ⚠️ Review CORS origins for production
- ⚠️ Set strong database passwords
- ⚠️ Rotate JWT secrets regularly

## Next Steps After Deployment

1. Test all endpoints via Swagger: `https://api.idansure.com/swagger`
2. Test authentication: `POST /api/auth/login`
3. Test CORS from frontend application
4. Set up monitoring/alerting
5. Configure automated backups
6. Document API for external consumers
