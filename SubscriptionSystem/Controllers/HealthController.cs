using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SubscriptionSystem.Infrastructure.Data;
using System.Threading;

namespace SubscriptionSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IDistributedCache _cache;
        private readonly IHostEnvironment _env;

        public HealthController(ApplicationDbContext db, IDistributedCache cache, IHostEnvironment env)
        {
            _db = db;
            _cache = cache;
            _env = env;
        }

        // Simple liveness probe: service is up and able to execute code.
        [HttpGet]
        [Route("")] // GET /api/health
        public IActionResult Get() => Ok(new { status = "ok", time = DateTime.UtcNow });

        // Readiness probe: verifies DB connectivity and Redis cache round-trip.
        [HttpGet("ready")] // GET /api/health/ready
        public async Task<IActionResult> Ready()
        {
            var checks = new List<object>();
            var overallOk = true;

            // DB check
            try
            {
                // lightweight metadata query
                var canConnect = await _db.Database.CanConnectAsync();
                checks.Add(new { component = "database", ok = canConnect });
                overallOk = overallOk && canConnect;
            }
            catch (Exception ex)
            {
                checks.Add(new { component = "database", ok = false, error = ex.Message });
                overallOk = false;
            }

            // Redis (distributed cache) check
            var skipRedisHealth = _env.IsDevelopment() ||
                                  string.Equals(Environment.GetEnvironmentVariable("SKIP_REDIS_HEALTH"), "true", StringComparison.OrdinalIgnoreCase);
            if (skipRedisHealth)
            {
                checks.Add(new { component = "cache", ok = true, note = "skipped" });
            }
            else
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    var key = "health:ping";
                    var existing = await _cache.GetStringAsync(key, cts.Token);
                    if (existing == null)
                    {
                        await _cache.SetStringAsync(key, DateTime.UtcNow.ToString("O"), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) }, cts.Token);
                    }
                    checks.Add(new { component = "cache", ok = true });
                }
                catch (Exception ex)
                {
                    checks.Add(new { component = "cache", ok = false, error = ex.Message });
                    overallOk = false;
                }
            }

            var payload = new { status = overallOk ? "ready" : "degraded", checks, time = DateTime.UtcNow };
            if (overallOk) return Ok(payload);
            return StatusCode(503, payload); // signal not ready to load balancer
        }
    }
}
