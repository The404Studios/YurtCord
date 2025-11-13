namespace YurtCord.API.Configuration;

/// <summary>
/// Configuration for embedded/self-contained mode
/// </summary>
public class EmbeddedModeConfiguration
{
    /// <summary>
    /// Enable embedded mode (SQLite, in-memory cache, local file storage)
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// SQLite database file path
    /// </summary>
    public string DatabasePath { get; set; } = "./Data/yurtcord.db";

    /// <summary>
    /// Local file storage path (instead of MinIO)
    /// </summary>
    public string FileStoragePath { get; set; } = "./Data/uploads";

    /// <summary>
    /// Auto-detect: If true, automatically enable embedded mode if PostgreSQL is not available
    /// </summary>
    public bool AutoDetect { get; set; } = true;
}
