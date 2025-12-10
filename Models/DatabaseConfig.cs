using System.Text.Json;

namespace Mailer.Models;

/// <summary>
/// Database connection configuration
/// </summary>
public class DatabaseConfig
{
    public string ServerIp { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseWindowsAuth { get; set; } = true;
    
    // Logging configuration
    public bool EnableFileLogging { get; set; } = true;
    public bool EnableDatabaseLogging { get; set; } = true;
    public bool EnableEmailHistory { get; set; } = true;
    public string FileLogLevel { get; set; } = "Information";
    public string DatabaseLogLevel { get; set; } = "Debug";

    /// <summary>
    /// Builds the connection string from the stored values
    /// </summary>
    public string GetConnectionString()
    {
        if (UseWindowsAuth)
        {
            return $"Server={ServerIp};Database={DatabaseName};Integrated Security=true;TrustServerCertificate=true;";
        }
        else
        {
            return $"Server={ServerIp};Database={DatabaseName};User Id={Username};Password={Password};TrustServerCertificate=true;";
        }
    }

    private static readonly string ConfigFilePath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "db.config.json"
    );

    /// <summary>
    /// Saves the configuration to the file
    /// </summary>
    /// <param name="applySecurityAttributes">Whether to apply Hidden, System, ReadOnly attributes</param>
    public void Save(bool applySecurityAttributes = true)
    {
        try
        {
            // Remove all security attributes if file exists
            if (File.Exists(ConfigFilePath))
            {
                FileInfo existingFile = new FileInfo(ConfigFilePath);
                // Set to Normal to remove all restrictive attributes
                existingFile.Attributes = FileAttributes.Normal;
            }

            // Write the JSON content
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);

            // Apply security attributes if requested
            if (applySecurityAttributes)
            {
                SetFileSecurityAttributes(ConfigFilePath);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException(
                $"Cannot write to configuration file. Please ensure you have write permissions to: {ConfigFilePath}", 
                ex);
        }
        catch (IOException ex)
        {
            throw new IOException(
                $"Error writing configuration file: {ConfigFilePath}", 
                ex);
        }
    }

    /// <summary>
    /// Sets security attributes on the configuration file
    /// </summary>
    private static void SetFileSecurityAttributes(string filePath)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(filePath);
            // Set as Hidden, System, and ReadOnly
            fileInfo.Attributes = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly;
        }
        catch (Exception ex)
        {
            // Log but don't fail if we can't set attributes
            Console.WriteLine($"Warning: Could not set security attributes on {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the database configuration from file
    /// </summary>
    public static DatabaseConfig? Load()
    {
        if (!File.Exists(ConfigFilePath))
            return null;

        try
        {
            // Temporarily remove readonly attribute to read the file
            FileInfo fileInfo = new FileInfo(ConfigFilePath);
            FileAttributes originalAttributes = fileInfo.Attributes;
            
            if ((originalAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                fileInfo.Attributes = originalAttributes & ~FileAttributes.ReadOnly;
            }

            string json = File.ReadAllText(ConfigFilePath);
            
            // Restore original attributes
            fileInfo.Attributes = originalAttributes;
            
            return JsonSerializer.Deserialize<DatabaseConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if configuration file exists
    /// </summary>
    public static bool Exists()
    {
        return File.Exists(ConfigFilePath);
    }
}
