using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Mailer.Models;
using Mailer.Services;

namespace Mailer;

/// <summary>
/// Interactive setup utility for creating and updating configuration in database
/// </summary>
public class ConfigSetup
{
    public static void Run()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        Mailer - Secure Configuration Setup Utility        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        // Step 1: Collect database connection details
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Step 1: Database Connection Setup");
        Console.ResetColor();
        Console.WriteLine();

        string serverIp = PromptForValue("Database Server IP or hostname (e.g., localhost, 192.168.1.100)", isSecret: false);
        string databaseName = PromptForValue("Database Name (e.g., MailerDB, EmailConfig)", isSecret: false);
        string tableName = PromptForValue("Table Name (e.g., MailerConfig, GraphCredentials)", isSecret: false);
        
        Console.WriteLine();
        Console.Write("Use Windows Authentication? (yes/no, default: yes): ");
        string? authResponse = Console.ReadLine()?.Trim().ToLower();
        bool useWindowsAuth = string.IsNullOrWhiteSpace(authResponse) || authResponse == "yes" || authResponse == "y";

        string username = string.Empty;
        string password = string.Empty;
        string connectionString;
        
        if (useWindowsAuth)
        {
            connectionString = $"Server={serverIp};Database={databaseName};Integrated Security=true;TrustServerCertificate=true;";
        }
        else
        {
            username = PromptForValue("SQL Server Username", isSecret: false);
            password = PromptForValue("SQL Server Password", isSecret: true);
            connectionString = $"Server={serverIp};Database={databaseName};User Id={username};Password={password};TrustServerCertificate=true;";
        }

        Console.WriteLine();
        Console.WriteLine("Testing database connection...");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Database connection successful!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Failed to connect to database");
            Console.WriteLine();
            Console.WriteLine("Error Details:");
            Console.WriteLine($"  Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"  Message: {(string.IsNullOrWhiteSpace(ex.Message) ? "(No message provided)" : ex.Message)}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Inner Exception Type: {ex.InnerException.GetType().Name}");
                Console.WriteLine($"  Inner Exception Message: {ex.InnerException.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Connection Details:");
            Console.WriteLine($"  Server: {serverIp}");
            Console.WriteLine($"  Database: {databaseName}");
            Console.WriteLine($"  Authentication: {(useWindowsAuth ? "Windows Authentication" : "SQL Server Authentication")}");
            if (!useWindowsAuth)
            {
                Console.WriteLine($"  Username: {username}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Full Exception Details:");
            Console.WriteLine(ex.ToString());
            
            Console.WriteLine();
            Console.WriteLine("Common Issues:");
            Console.WriteLine("  • Server name or IP address is incorrect");
            Console.WriteLine("  • SQL Server is not running or not accessible");
            Console.WriteLine("  • Firewall is blocking the connection (port 1433)");
            Console.WriteLine("  • Database does not exist");
            Console.WriteLine("  • Authentication credentials are incorrect");
            Console.WriteLine("  • SQL Server is not configured for remote connections");
            
            Console.ResetColor();
            return;
        }

        // Save database configuration for future use
        var dbConfig = new DatabaseConfig
        {
            ServerIp = serverIp,
            DatabaseName = databaseName,
            TableName = tableName,
            UseWindowsAuth = useWindowsAuth,
            Username = username,
            Password = password
        };

        Console.WriteLine();
        Console.Write("Do you want to secure the db.config.json file (Hidden, System, ReadOnly)? (yes/no, default: no): ");
        string? secureFileInput = Console.ReadLine()?.Trim().ToLower();
        bool secureFile = secureFileInput == "yes" || secureFileInput == "y";

        dbConfig.Save(applySecurityAttributes: secureFile);

        if (secureFile)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Note: db.config.json will be marked as Hidden, System, and ReadOnly.");
            Console.WriteLine("This provides extra security but may not be copied when copying the folder.");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Note: db.config.json will be a normal visible file.");
            Console.WriteLine("This makes it easy to copy with the folder but is less secure.");
            Console.ResetColor();
        }

        // Set connection string and table name for SecureConfigService
        SecureConfigService.SetConnectionString(connectionString);
        SecureConfigService.SetTableName(tableName);

        // Initialize database table
        Console.WriteLine();
        Console.WriteLine("Initializing database table...");
        try
        {
            SecureConfigService.InitializeDatabase();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Table '{tableName}' initialized successfully");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Failed to initialize table: {ex.Message}");
            Console.ResetColor();
            return;
        }
        Console.WriteLine();

        // Step 2: Check if config already exists
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Step 2: Microsoft Graph API Configuration");
        Console.ResetColor();
        Console.WriteLine();

        // Check if config already exists
        bool configExists = SecureConfigService.ConfigExists();
        if (configExists)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ Warning: A configuration already exists in the database.");
            Console.WriteLine($"Location: {SecureConfigService.GetConfigFilePath()}");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write("Do you want to overwrite it? (yes/no): ");
            string? response = Console.ReadLine()?.Trim().ToLower();
            
            if (response != "yes" && response != "y")
            {
                Console.WriteLine("\nSetup cancelled.");
                return;
            }
            Console.WriteLine();
        }

        Console.WriteLine("Please provide your Microsoft Graph API configuration:");
        Console.WriteLine("(This information will be stored securely in the database)");
        Console.WriteLine();

        // Collect configuration values
        var config = new GraphConfig
        {
            TenantId = PromptForValue("Azure Tenant ID", isSecret: false),
            ClientId = PromptForValue("Azure Client ID (Application ID)", isSecret: false),
            ClientSecret = PromptForValue("Azure Client Secret", isSecret: true),
            SenderEmail = PromptForEmail("Sender Email Address"),
            Recipients = PromptForValue("Default Recipients (optional, comma-separated)", isSecret: false, allowEmpty: true)
        };

        Console.WriteLine();
        Console.WriteLine("Saving configuration to database...");

        try
        {
            // Save to database
            SecureConfigService.SaveConfig(config);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Configuration saved successfully!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"Database configuration stored in: {Path.Combine(Directory.GetCurrentDirectory(), "db.config.json")}");
            Console.WriteLine($"Graph API credentials stored in: SQL Server Database: {dbConfig.ServerIp} / {dbConfig.DatabaseName} / Table: {dbConfig.TableName}");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Security features:");
            Console.WriteLine("  Database Configuration File (db.config.json):");
            Console.WriteLine("    ✓ File marked as Hidden");
            Console.WriteLine("    ✓ File marked as System file");
            Console.WriteLine("    ✓ File set to Read-only");
            Console.WriteLine("  Graph API Credentials:");
            Console.WriteLine("    ✓ Stored in SQL Server database");
            Console.WriteLine("    ✓ Protected by database authentication and permissions");
            Console.WriteLine("    ✓ Accessible only with valid database credentials");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("You can now run the mailer application:");
            Console.WriteLine("  dotnet run -- --to recipient@example.com --subject \"Test\" --body \"<h1>Hello</h1>\"");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Error saving configuration: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static string PromptForValue(string fieldName, bool isSecret, bool allowEmpty = false)
    {
        while (true)
        {
            Console.Write($"{fieldName}: ");
            
            string? value;
            if (isSecret)
            {
                value = ReadPassword();
            }
            else
            {
                value = Console.ReadLine();
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            if (allowEmpty)
            {
                return string.Empty;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ {fieldName} cannot be empty. Please try again.");
            Console.ResetColor();
        }
    }

    private static string PromptForEmail(string fieldName)
    {
        while (true)
        {
            Console.Write($"{fieldName}: ");
            string? value = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(value) && IsValidEmail(value.Trim()))
            {
                return value.Trim();
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Please enter a valid email address.");
            Console.ResetColor();
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    private static string ReadPassword()
    {
        var password = new System.Text.StringBuilder();
        
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        }
        
        return password.ToString();
    }
}
