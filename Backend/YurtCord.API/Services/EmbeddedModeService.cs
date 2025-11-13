using System.Net.Sockets;
using Npgsql;
using Serilog;
using YurtCord.API.Configuration;

namespace YurtCord.API.Services;

/// <summary>
/// Service to manage embedded/self-contained mode
/// </summary>
public class EmbeddedModeService
{
    private readonly EmbeddedModeConfiguration _config;

    public EmbeddedModeService(EmbeddedModeConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Determines if embedded mode should be used
    /// </summary>
    public bool ShouldUseEmbeddedMode(string? connectionString)
    {
        // If explicitly enabled, use embedded mode
        if (_config.Enabled)
        {
            Log.Information("Embedded mode explicitly enabled");
            return true;
        }

        // If auto-detect is enabled, check if PostgreSQL is available
        if (_config.AutoDetect && !string.IsNullOrEmpty(connectionString))
        {
            if (!IsPostgreSQLAvailable(connectionString))
            {
                Log.Warning("PostgreSQL not available. Automatically switching to embedded mode (SQLite)");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if PostgreSQL is available and connectable
    /// </summary>
    private bool IsPostgreSQLAvailable(string connectionString)
    {
        try
        {
            // Parse connection string to get host and port
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var host = builder.Host ?? "localhost";
            var port = builder.Port;

            // Try to connect to the PostgreSQL port
            using var client = new TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));

            if (success)
            {
                client.EndConnect(result);
                Log.Information("PostgreSQL detected at {Host}:{Port}", host, port);
                return true;
            }

            Log.Warning("Cannot connect to PostgreSQL at {Host}:{Port}", host, port);
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error checking PostgreSQL availability");
            return false;
        }
    }

    /// <summary>
    /// Ensure embedded mode directories exist
    /// </summary>
    public void EnsureDirectoriesExist()
    {
        var dbDirectory = Path.GetDirectoryName(_config.DatabasePath);
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
            Log.Information("Created database directory: {Path}", dbDirectory);
        }

        if (!Directory.Exists(_config.FileStoragePath))
        {
            Directory.CreateDirectory(_config.FileStoragePath);
            Log.Information("Created file storage directory: {Path}", _config.FileStoragePath);
        }
    }
}
