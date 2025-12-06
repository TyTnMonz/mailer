using Microsoft.Data.SqlClient;
using Mailer.Models;
using Serilog;

namespace Mailer.Services;

/// <summary>
/// Provides secure configuration management using SQL Server database with key-value storage
/// </summary>
public class SecureConfigService
{
    private static string? _connectionString;
    private static string _tableName = "MailerConfig"; // Default table name
    private static readonly ILogger _logger = Log.Logger;

    /// <summary>
    /// Sets the database connection string
    /// </summary>
    public static void SetConnectionString(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Sets the table name to use for configuration storage
    /// </summary>
    public static void SetTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be empty", nameof(tableName));
        
        _tableName = tableName;
        _logger.Information("Configuration table name set to: {TableName}", _tableName);
    }

    /// <summary>
    /// Initializes the database table if it doesn't exist
    /// </summary>
    public static void InitializeDatabase()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Connection string not set. Call SetConnectionString first.");

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string createTableSql = $@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{_tableName}')
                BEGIN
                    CREATE TABLE [{_tableName}] (
                        SGIndice INT PRIMARY KEY IDENTITY(1,1),
                        SGTipo NVARCHAR(100) NOT NULL UNIQUE,
                        SGValore NVARCHAR(MAX) NOT NULL
                    );
                END";

            using var command = new SqlCommand(createTableSql, connection);
            command.ExecuteNonQuery();

            _logger.Information("Database table '{TableName}' initialized successfully", _tableName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize database table '{TableName}'", _tableName);
            throw new InvalidOperationException($"Failed to initialize database: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves the configuration to the database as key-value pairs
    /// </summary>
    /// <param name="config">The GraphConfig to save</param>
    public static void SaveConfig(GraphConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Connection string not set. Call SetConnectionString first.");

        // Validate configuration
        if (string.IsNullOrWhiteSpace(config.TenantId) ||
            string.IsNullOrWhiteSpace(config.ClientId) ||
            string.IsNullOrWhiteSpace(config.ClientSecret) ||
            string.IsNullOrWhiteSpace(config.SenderEmail))
        {
            throw new ArgumentException("All required configuration fields must be provided");
        }

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // Define key-value pairs
            var configValues = new Dictionary<string, string>
            {
                { "TenantID", config.TenantId },
                { "ClientID", config.ClientId },
                { "ClientSec", config.ClientSecret },
                { "Sender", config.SenderEmail },
                { "To", config.Recipients ?? string.Empty }
            };

            foreach (var kvp in configValues)
            {
                // Check if key exists
                string checkSql = $"SELECT COUNT(*) FROM [{_tableName}] WHERE SGTipo = @Key";
                using var checkCommand = new SqlCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@Key", kvp.Key);
                int count = (int)checkCommand.ExecuteScalar();

                string sql;
                if (count > 0)
                {
                    // Update existing value
                    sql = $"UPDATE [{_tableName}] SET SGValore = @Value WHERE SGTipo = @Key";
                    _logger.Debug("Updating configuration key: {Key}", kvp.Key);
                }
                else
                {
                    // Insert new value
                    sql = $"INSERT INTO [{_tableName}] (SGTipo, SGValore) VALUES (@Key, @Value)";
                    _logger.Debug("Inserting configuration key: {Key}", kvp.Key);
                }

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Key", kvp.Key);
                command.Parameters.AddWithValue("@Value", kvp.Value);
                command.ExecuteNonQuery();
            }

            _logger.Information("Configuration saved successfully to database (5 key-value pairs)");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save configuration to database");
            throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Loads the configuration from the database
    /// </summary>
    /// <returns>The GraphConfig</returns>
    public static GraphConfig LoadConfig()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Connection string not set. Call SetConnectionString first.");

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // Load all key-value pairs
            string sql = $"SELECT SGTipo, SGValore FROM [{_tableName}]";
            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            var configData = new Dictionary<string, string>();
            while (reader.Read())
            {
                string key = reader.GetString(0);
                string value = reader.GetString(1);
                configData[key] = value;
            }

            // Validate that all required keys exist
            string[] requiredKeys = { "TenantID", "ClientID", "ClientSec", "Sender" };
            var missingKeys = requiredKeys.Where(k => !configData.ContainsKey(k)).ToList();

            if (missingKeys.Any())
            {
                throw new InvalidOperationException(
                    $"Missing required configuration keys: {string.Join(", ", missingKeys)}\n" +
                    "Please run the setup utility first:\n" +
                    "  dotnet run -- --setup"
                );
            }

            var config = new GraphConfig
            {
                TenantId = configData["TenantID"],
                ClientId = configData["ClientID"],
                ClientSecret = configData["ClientSec"],
                SenderEmail = configData["Sender"],
                Recipients = configData.ContainsKey("To") ? configData["To"] : string.Empty
            };

            _logger.Information("Configuration loaded successfully from database for sender: {SenderEmail}", config.SenderEmail);

            return config;
        }
        catch (SqlException ex)
        {
            _logger.Error(ex, "SQL error while loading configuration from database");
            throw new InvalidOperationException(
                $"Failed to load configuration from database: {ex.Message}\n" +
                "Please check your database connection and run the setup utility:\n" +
                "  dotnet run -- --setup",
                ex
            );
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.Error(ex, "Unexpected error while loading configuration");
            throw new InvalidOperationException($"Failed to load configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if configuration exists in the database
    /// </summary>
    public static bool ConfigExists()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return false;

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // Check if all required keys exist
            string sql = $@"
                SELECT COUNT(*) 
                FROM [{_tableName}] 
                WHERE SGTipo IN ('TenantID', 'ClientID', 'ClientSec', 'Sender')";
            
            using var command = new SqlCommand(sql, connection);
            int count = (int)command.ExecuteScalar();

            // Configuration exists if all 4 keys are present
            return count == 4;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a description of where the configuration is stored
    /// </summary>
    public static string GetConfigFilePath()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return "Database connection not configured";

        // Extract server and database from connection string for display
        try
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            return $"SQL Server Database: {builder.DataSource} / {builder.InitialCatalog} / Table: {_tableName} (Key-Value pairs)";
        }
        catch
        {
            return $"SQL Server Database ({_tableName} table - Key-Value storage)";
        }
    }
}
