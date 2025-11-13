using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YurtCord.Infrastructure.Data;

namespace YurtCord.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(YurtCordDbContext context) : ControllerBase
{
    private readonly YurtCordDbContext _context = context;

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }

    /// <summary>
    /// Detailed health check with service status
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedHealth()
    {
        var health = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            services = new
            {
                database = await CheckDatabaseHealthAsync(),
                api = new { status = "healthy", responseTime = "< 1ms" }
            },
            uptime = GetUptime()
        };

        return Ok(health);
    }

    /// <summary>
    /// Liveness probe for Kubernetes
    /// </summary>
    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        return Ok(new { status = "alive" });
    }

    /// <summary>
    /// Readiness probe for Kubernetes
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        var dbHealth = await CheckDatabaseHealthAsync();

        if (dbHealth.status != "healthy")
        {
            return StatusCode(503, new
            {
                status = "not ready",
                reason = "Database connection failed"
            });
        }

        return Ok(new { status = "ready" });
    }

    private async Task<object> CheckDatabaseHealthAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new
            {
                status = "healthy",
                responseTime = $"{duration:F2}ms",
                provider = _context.Database.ProviderName
            };
        }
        catch (Exception ex)
        {
            return new
            {
                status = "unhealthy",
                error = ex.Message
            };
        }
    }

    private static string GetUptime()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }
}
