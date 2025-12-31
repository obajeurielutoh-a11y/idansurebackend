using DotNetEnv;
using System.IO;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.Services;
using SubscriptionSystem.Infrastructure.Repositories;
using SubscriptionSystem.Infrastructure.Services;
using AspNetCoreRateLimit;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using SubscriptionSystem.Infrastructure.Hubs;
using SubscriptionSystem.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using SubscriptionSystem.Infrastructure.Middleware;
using SubscriptionSystem.Application.Extensions;
using SubscriptionSystem.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication;
using SubscriptionSystem.Infrastructure.Authentication;
using SubscriptionSystem.Infrastructure.Data;
using System.Security.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.HttpsPolicy;
using System.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using StackExchange.Redis;
// Robustly load .env (search up parent directories) BEFORE building configuration
string? FindDotEnv()
{
    var dir = Directory.GetCurrentDirectory();
    while (!string.IsNullOrEmpty(dir))
    {
        var candidate = Path.Combine(dir, ".env");
        if (File.Exists(candidate)) return candidate;
        dir = Path.GetDirectoryName(dir);
    }
    return null;
}

var dotenvPath = FindDotEnv();
if (!string.IsNullOrEmpty(dotenvPath))
{
    DotNetEnv.Env.Load(dotenvPath);
    Console.WriteLine($"Loaded .env from: {dotenvPath}");
}
else
{
    Console.WriteLine(".env file not found in current or parent directories.");
}

// Quick diagnostic (does NOT print secret values)
var jwtPresent = Environment.GetEnvironmentVariable("Jwt__Key");
Console.WriteLine($"Jwt__Key present: { (jwtPresent != null ? "yes" : "no") }, length: { (jwtPresent != null ? jwtPresent.Length.ToString() : "0") }");

var builder = WebApplication.CreateBuilder(args);
// Add services to the container

// Rate Limiting Configuration
builder.Services.AddMemoryCache();
builder.Services.AddOptions();
// Bind IpRateLimiting and policy sections if present (safe even if sections are missing)
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
// In-memory stores for counters and rules
builder.Services.AddInMemoryRateLimiting();
// Use async key-lock processing strategy for better concurrency
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
// Core rate limit configuration helper
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

builder.Services.AddSignalR();
// Register Application services
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IVerifiedEmailRepository, VerifiedEmailRepository>();

builder.Services.AddScoped<ITicketService, TicketService>();
//builder.Services.AddHostedService<SubscriptionExpirationReminderService>();

builder.Services.AddScoped<IAsedeyhotPredictionService, AsedeyhotPredictionService>();
builder.Services.AddScoped<IAsedeyhotPredictionRepository, AsedeyhotPredictionRepository>();
builder.Services.AddScoped<IPredictionPostService, PredictionPostService>();


// Register Infrastructure services
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPredictionRepository, PredictionRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IGroupChatService, GroupChatService>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IAuthorizationHandler, ApiKeyAuthorizationHandler>();
builder.Services.AddScoped<IJwtService, JwtService>();
// AI Chat Provider (always real OpenAI for runtime)
builder.Services.AddScoped<IAiChatProvider, OpenAiChatProvider>();
// Audio (AWS Polly) & Transcription (OpenAI Whisper)
builder.Services.AddScoped<IAudioService, AwsPollyAudioService>();
builder.Services.AddScoped<ITranscriptionService, OpenAiTranscriptionService>();
builder.Services.AddScoped<ISmsService, SmsService>();
    
builder.Services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
builder.Services.AddScoped<IWhatsAppProvider, WhatsAppCloudProvider>();
// Admin WhatsApp webhook services
builder.Services.AddScoped<SubscriptionSystem.Infrastructure.Services.WhatsAppAdminCommandParser>();
builder.Services.AddScoped<SubscriptionSystem.Infrastructure.Services.WhatsAppAdminPredictionService>();
// Prediction notification service (sends WhatsApp updates to active subscribers)
builder.Services.AddScoped<SubscriptionSystem.Infrastructure.Services.PredictionNotificationService>();
// PraisonAI WhatsApp Agent service (verifies numbers, manages agent-driven notifications)
builder.Services.AddScoped<SubscriptionSystem.Infrastructure.Services.PraisonAIWhatsAppAgentService>();
// WhatsApp MCP adapter for creating/verifying MCP recipients
builder.Services.AddScoped<SubscriptionSystem.Infrastructure.Services.WhatsAppMCPAdapter>();
// Webhook replay protector (uses IDistributedCache - Redis if configured)
builder.Services.AddScoped<SubscriptionSystem.Infrastructure.Services.WebhookReplayProtector>();
builder.Services.AddScoped<IDomainEventHandler<SubscriptionSystem.Domain.Events.SubscriptionActivatedEvent>, SubscriptionSystem.Application.Services.Handlers.SubscriptionActivatedHandler>();
builder.Services.AddScoped<IDomainEventHandler<SubscriptionSystem.Domain.Events.TipPostedEvent>, SubscriptionSystem.Application.Services.Handlers.TipPostedHandler>();
// MCP tool registry (WebSocket JSON-RPC style) - scoped to align with scoped application services
builder.Services.AddScoped<SubscriptionSystem.Infrastructure.Mcp.IMcpToolRegistry, SubscriptionSystem.Infrastructure.Mcp.McpToolRegistry>();
// Message analysis (tone, scope, context auto-derivation)
builder.Services.AddScoped<SubscriptionSystem.Application.Interfaces.IMessageAnalysisService, SubscriptionSystem.Application.Services.MessageAnalysisService>();
// Language detection used by OpenAiChatProvider
builder.Services.AddScoped<SubscriptionSystem.Infrastructure.Services.LanguageDetectionService>();

// Prediction analytics service (monthly/daily metrics, moving averages, trend)
builder.Services.AddScoped<SubscriptionSystem.Application.Interfaces.IPredictionAnalyticsService, SubscriptionSystem.Application.Services.PredictionAnalyticsService>();


var dbConnectionString = builder.Configuration.GetConnectionString("ConnectionStrings__IdanSurestSecurityConnectionForPrediction")
    ?? builder.Configuration["ConnectionStrings:IdanSurestSecurityConnectionForPrediction"]
    ?? throw new InvalidOperationException("Database connection string 'IdanSurestSecurityConnectionForPrediction' is not configured.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dbConnectionString, sql =>
    {
        // Enable transient fault handling for PostgreSQL connectivity blips
        sql.EnableRetryOnFailure(maxRetryCount: 5,
                                 maxRetryDelay: TimeSpan.FromSeconds(10),
                                 errorCodesToAdd: null);
        sql.CommandTimeout(60);
    }));

// Repositories
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
// Add other repositories...

// Services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
// Register Shared services (if needed)
builder.Services.AddSingleton<ISharedService, SharedService>();
builder.Services.AddScoped<ITokenService, TokenService>();
// Register Entity Framework DbContext
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

// Distributed cache: prefer Redis in non-Development; fall back to in-memory in Development
var useRedisInDev = builder.Configuration.GetValue<bool>("UseRedisInDev", false);
if (!builder.Environment.IsDevelopment() || useRedisInDev)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        var connString = builder.Configuration.GetConnectionString("RedisConnection")
            ?? builder.Configuration["ConnectionStrings:RedisConnection"]
            ?? string.Empty;

        // Configure robust client timeouts and non-fatal connect behavior
    var config = ConfigurationOptions.Parse(connString);
    config.AllowAdmin = false;
        config.AbortOnConnectFail = false;
        config.ConnectTimeout = 1500; // ms
        config.SyncTimeout = 3000;    // ms
        config.KeepAlive = 15;        // seconds
        options.ConfigurationOptions = config;
        options.InstanceName = "SubscriptionSystem:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

//Register HttpClient
builder.Services.AddHttpClient();


builder.Services.AddHttpClient("Credo", client =>
{
    client.BaseAddress = new Uri("https://api.credocentral.com/");
    client.DefaultRequestHeaders.Add("Authorization", builder.Configuration["Credo:ApiKey"]);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("PaystackClient", client =>
{
    var paystackBaseUrl = builder.Configuration["Paystack:BaseUrl"]
        ?? throw new InvalidOperationException("Paystack:BaseUrl configuration missing");
    client.BaseAddress = new Uri(paystackBaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});
//Add Alatpay HttpClient
builder.Services.AddHttpClient("AlatpayClient", client =>
{
    var alatpayBaseUrl = builder.Configuration["Alatpay:BaseUrl"]
        ?? throw new InvalidOperationException("Alatpay:BaseUrl configuration missing");
    var alatpaySecret = builder.Configuration["Alatpay:SecretKey"]
        ?? throw new InvalidOperationException("Alatpay:SecretKey configuration missing");
    client.BaseAddress = new Uri(alatpayBaseUrl);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {alatpaySecret}");
});
//builder.Services.AddScoped<IPaymentService, PaymentService>();
// Configure Azure Service Bus
builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var serviceBusConnectionString = builder.Configuration.GetConnectionString("ServiceBusConnection");
    return new ServiceBusClient(serviceBusConnectionString);
});

builder.Services.AddSingleton<ServiceBusSender>(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateSender("emailQueue"); // Replace 'emailQueue' with your queue name
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
            policy.WithOrigins("http://localhost:3000",
                               "https://idansure.com",
                               "https://www.idansure.com"
                               
                        ) // Allow only these origins
                               
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Add this to allow credentials
    });
        
        // Wide-open for development to avoid mixed content/CORS friction
        options.AddPolicy("AllowDevAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
            // Note: Do not use AllowCredentials with AllowAnyOrigin
        });
});
builder.Services.AddScoped<IGroupChatService, GroupChatService>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();

// JWT Authentication Configuration
// Read JWT configuration from configuration (appsettings / env) or common env names as fallback
string? GetConfigOrEnv(string configKey, string[] envFallbacks)
{
    var v = builder.Configuration[configKey];
    if (!string.IsNullOrEmpty(v)) return v;
    foreach (var name in envFallbacks)
    {
        var ev = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrEmpty(ev)) return ev;
    }
    return null;
}

var jwtKey = GetConfigOrEnv("Jwt:Key", new[] { "Jwt__Key", "JWT_KEY" });
var jwtIssuer = GetConfigOrEnv("Jwt:Issuer", new[] { "Jwt__Issuer", "JWT_ISSUER" });
var jwtAudience = GetConfigOrEnv("Jwt:Audience", new[] { "Jwt__Audience", "JWT_AUDIENCE" });

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    var guidance = "Missing JWT configuration. Set these environment variables in your host:\n" +
                   " - Jwt__Key (or JWT_KEY)\n" +
                   " - Jwt__Issuer (or JWT_ISSUER)\n" +
                   " - Jwt__Audience (or JWT_AUDIENCE)\n" +
                   "On Render: go to your service -> Environment -> Environment Variables, and add them there.\n" +
                   "For local development you can keep a .env file with keys using double-underscore (Jwt__Key=...).";

    // Log guidance to console for deploy troubleshooting (safe to expose guidance but not secret values)
    Console.Error.WriteLine(guidance);
    throw new InvalidOperationException("JWT configuration is incomplete. Please check environment variables or appsettings.json.");
}
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Prefer Authorization header (standard) and fall back to cookie for web clients
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }

                if (string.IsNullOrEmpty(context.Token))
                {
                    // Support both legacy cookie name "token" and newer "access_token"
                    var cookieToken = context.Request.Cookies["access_token"];
                    var legacyToken = context.Request.Cookies["token"];
                    context.Token = !string.IsNullOrEmpty(cookieToken) ? cookieToken : legacyToken;
                }

                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer("AdminBearer", options =>
    {
        var adminIssuer = builder.Configuration["Jwt:AdminIssuer"] ?? jwtIssuer;
        var adminAudience = builder.Configuration["Jwt:AdminAudience"] ?? jwtAudience;
        var adminKey = builder.Configuration["Jwt:AdminKey"] ?? jwtKey;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ValidIssuer = adminIssuer,
            ValidAudience = adminAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }

                if (string.IsNullOrEmpty(context.Token))
                {
                    var cookieToken = context.Request.Cookies["access_token"];
                    var legacyToken = context.Request.Cookies["token"];
                    context.Token = !string.IsNullOrEmpty(cookieToken) ? cookieToken : legacyToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// Add controllers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddControllers();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]
        ?? throw new InvalidOperationException("Authentication:Google:ClientId configuration missing");
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
        ?? throw new InvalidOperationException("Authentication:Google:ClientSecret configuration missing");
});
// Swagger setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Explicitly register a Swagger document
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IdanSureSubscription API",
        Version = "v1",
        Description = "IdanSureSubscription backend API"
    });

    // Default user JWT scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Paste only the token (no 'Bearer ' prefix).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Admin JWT scheme
    c.AddSecurityDefinition("AdminBearer", new OpenApiSecurityScheme
    {
        Description = "Admin JWT Authorization. Paste only the token (no 'Bearer ' prefix).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Allow using either scheme globally; endpoints can still specify which scheme via attributes
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "AdminBearer" }
            },
            Array.Empty<string>()
        }
    });

    // Inject X-User-Id / X-Admin-Id headers where policies require them
    c.OperationFilter<SubscriptionSystem.Infrastructure.Swagger.AddAuthHeadersOperationFilter>();
});
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .AddXmlSerializerFormatters();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

});
builder.Services.AddHttpContextAccessor();
// Register custom authorization handlers
builder.Services.AddSingleton<IAuthorizationHandler, SubscriptionSystem.Infrastructure.Authorization.HeaderMatchesClaimHandler>();
// Add API key authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKeyPolicy", policy =>
        policy.Requirements.Add(new ApiKeyRequirement()));

    // Require X-User-Id header to match NameIdentifier and token_type=user
    options.AddPolicy("UserWithIdHeader", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new SubscriptionSystem.Infrastructure.Authorization.HeaderMatchesClaimRequirement("X-User-Id", ClaimTypes.NameIdentifier, expectedTokenType: "user")));

    // Require X-Admin-Id header to match NameIdentifier and token_type=admin
    options.AddPolicy("AdminWithIdHeader", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin", "SuperAdmin")
              .AddRequirements(new SubscriptionSystem.Infrastructure.Authorization.HeaderMatchesClaimRequirement("X-Admin-Id", ClaimTypes.NameIdentifier, expectedTokenType: "admin")));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);
// In Program.cs or Startup.cs
OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
// Add application services


// Configure Kestrel to use specific TLS versions
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});

// Force HTTPS
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 443;
});

// Respect reverse proxy headers (X-Forwarded-Proto/For/Host) for correct https scheme behind ALB/Nginx/CloudFront
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor 
                             | ForwardedHeaders.XForwardedProto 
                             | ForwardedHeaders.XForwardedHost;
    // If running behind AWS/CloudFront/Nginx, we typically don't know the proxy addresses at build time
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = 2;
    
    // Allow forwarding from any proxy (required for CloudFront/ALB/Nginx)
    options.AllowedHosts.Clear();
});
// Add application and infrastructure services
// Build the application
var app = builder.Build();

// Remove stray characters inserted accidentally
// Enable WebSockets for MCP server endpoint
// --- Revised middleware ordering for correct CORS / routing / HTTPS behavior ---
// Apply EF Core migrations automatically only when explicitly enabled
// Set environment variable EF_AUTO_MIGRATE=true to enable in controlled environments
var autoMigrate = Environment.GetEnvironmentVariable("EF_AUTO_MIGRATE");
if (string.Equals(autoMigrate, "true", StringComparison.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<SubscriptionSystem.Infrastructure.Data.ApplicationDbContext>();
            db.Database.Migrate();
            app.Logger.LogInformation("EF Core migrations applied at startup because EF_AUTO_MIGRATE=true");
        }
        catch (Exception migrateEx)
        {
            app.Logger.LogError(migrateEx, "Database migration failed at startup");
            // Optionally rethrow to fail fast in prod
        }
    }
}
app.UseWebSockets();
app.UseSession();
// Apply forwarded headers early so downstream middleware (Swagger, redirection) sees correct scheme/host
app.UseForwardedHeaders();
// Ensure Swagger documents point to the current scheme/host to avoid mixed-content issues in dev
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        // Prefer forwarded headers set by reverse proxies (ALB/Nginx) to determine public scheme/host
        var proto = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault();
        var forwardedHost = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault();

        // Allow explicit override via configuration (PublicUrl) if set
        var configuredPublicUrl = builder.Configuration["PublicUrl"];
        if (!string.IsNullOrEmpty(configuredPublicUrl))
        {
            swaggerDoc.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
            {
                new Microsoft.OpenApi.Models.OpenApiServer { Url = configuredPublicUrl }
            };
            return;
        }

        var scheme = !string.IsNullOrEmpty(proto) ? proto : httpReq.Scheme;
        var host = !string.IsNullOrEmpty(forwardedHost) ? forwardedHost : httpReq.Host.Value;
        var serverUrl = $"{scheme}://{host}";
        swaggerDoc.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
        {
            new Microsoft.OpenApi.Models.OpenApiServer { Url = serverUrl }
        };
    });
});



 app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IdanSureSubscription V1");
        // c.RoutePrefix = string.Empty;  // Make Swagger UI the default page at root path
    });
// Rate limiting early
app.UseIpRateLimiting();

// Only force HTTPS outside Development (avoids local mixed-content issues)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
// Swagger/UI already configured above with PreSerializeFilters and UI setup; avoid duplicate calls


// Routing must come before CORS
app.UseRouting();

// CORS before auth/endpoints
app.UseCors(app.Environment.IsDevelopment() ? "AllowDevAll" : "AllowSpecificOrigins");

// Auth pipeline: authenticate first, then custom JWT enrichment, then authorize
app.UseAuthentication();
app.UseMiddleware<JwtMiddleware>();
app.UseAuthorization();
// --- End revised ordering ---

// Map controllers to routes
app.MapControllers();
app.MapHub<PredictionHub>("/predictionHub");
// Map MCP WebSocket endpoint
app.Map("/mcp", SubscriptionSystem.Infrastructure.Mcp.McpWebSocketHandler.HandleAsync);
// Start the application
app.Run();



