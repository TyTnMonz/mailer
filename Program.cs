using Mailer.Models;
using Mailer.Services;
using Serilog;
using System.Reflection;

namespace Mailer;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Initialize Serilog with daily rolling file and console output
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/mailer-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30)
            .CreateLogger();

        try
        {
            Log.Information("=== Mailer Application Started ===");
            Log.Information("Command line arguments: {Args}", string.Join(" ", args));

            // Parse command-line arguments manually
            var parsedArgs = ParseArguments(args);


            // Check for setup command
            if (parsedArgs.ContainsKey("--setup"))
            {
                Log.Information("Running configuration setup utility");
                ConfigSetup.Run();
                Log.Information("Setup utility completed");
                return 0;
            }

            // Check for version command
            if (parsedArgs.ContainsKey("--version") || parsedArgs.ContainsKey("-v"))
            {
                ShowVersion();
                Log.Information("Version information displayed");
                return 0;
            }

            if (parsedArgs.ContainsKey("--help") || parsedArgs.ContainsKey("-h"))
            {
                ShowHelp();
                Log.Information("Help displayed to user");
                return 0;
            }

            // Validate required arguments
            if (!parsedArgs.ContainsKey("--to"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: --to is required");
                Console.ResetColor();
                Log.Error("Missing required argument: --to");
                ShowHelp();
                return 1;
            }

            if (!parsedArgs.ContainsKey("--subject"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: --subject is required");
                Console.ResetColor();
                Log.Error("Missing required argument: --subject");
                ShowHelp();
                return 1;
            }

            if (!parsedArgs.ContainsKey("--body"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: --body is required");
                Console.ResetColor();
                Log.Error("Missing required argument: --body");
                ShowHelp();
                return 1;
            }

            // Extract values
            var to = parsedArgs["--to"];
            var cc = parsedArgs.ContainsKey("--cc") ? parsedArgs["--cc"] : Array.Empty<string>();
            var bcc = parsedArgs.ContainsKey("--bcc") ? parsedArgs["--bcc"] : Array.Empty<string>();
            var subject = parsedArgs["--subject"][0];
            var body = parsedArgs["--body"][0];
            var bodyIsFile = parsedArgs.ContainsKey("--body-is-file");
            var attachments = parsedArgs.ContainsKey("--attachments") ? parsedArgs["--attachments"] : null;
            var fromEmail = parsedArgs.ContainsKey("--from") ? parsedArgs["--from"][0] : null;

            Log.Information("Email parameters - To: {To}, Subject: {Subject}, BodyIsFile: {BodyIsFile}, Attachments: {AttachmentCount}, From: {From}",
                string.Join(", ", to), subject, bodyIsFile, attachments?.Length ?? 0, fromEmail ?? "(using default)");

            // Load database configuration
            Log.Information("Loading database configuration");
            var dbConfig = DatabaseConfig.Load();
            if (dbConfig == null || string.IsNullOrWhiteSpace(dbConfig.ServerIp) || 
                string.IsNullOrWhiteSpace(dbConfig.DatabaseName) || 
                string.IsNullOrWhiteSpace(dbConfig.TableName))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Database connection not configured.");
                Console.WriteLine("Please run the setup utility first to configure database connection:");
                Console.WriteLine("  dotnet run -- --setup");
                Console.ResetColor();
                Log.Error("Database configuration not found");
                return 1;
            }

            // Build connection string from config values
            string connectionString = dbConfig.GetConnectionString();
            
            // Reconfigure Serilog to add database sink
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/mailer-.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 30)
                .WriteTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
                    {
                        TableName = "MailerLogs",
                        AutoCreateSqlTable = true,
                        BatchPostingLimit = 1
                    })
                .Enrich.WithProperty("Application", "Mailer")
                .CreateLogger();
            
            SecureConfigService.SetConnectionString(connectionString);
            SecureConfigService.SetTableName(dbConfig.TableName);
            Log.Information("Database connection configured: Server={Server}, Database={Database}, Table={Table}", 
                dbConfig.ServerIp, dbConfig.DatabaseName, dbConfig.TableName);
            Log.Information("Serilog database sink configured for table: MailerLogs");

            // Load protected configuration from database
            Log.Information("Loading protected configuration from database");
            GraphConfig graphConfig;
            
            try
            {
                graphConfig = SecureConfigService.LoadConfig();
                Log.Information("Configuration loaded successfully. Sender: {SenderEmail}", graphConfig.SenderEmail);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Connection string not set"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Database connection not configured.");
                Console.WriteLine("Please run the setup utility first to configure database connection:");
                Console.WriteLine("  dotnet run -- --setup");
                Console.ResetColor();
                Log.Error(ex, "Database connection not configured");
                return 1;
            }
            catch (FileNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Protected configuration file not found.");
                Console.WriteLine("Please run the setup utility first:");
                Console.WriteLine("  dotnet run -- --setup");
                Console.ResetColor();
                Log.Error(ex, "Protected configuration file not found");
                return 1;
            }
            catch (InvalidOperationException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Failed to load configuration.");
                Console.WriteLine("The configuration file may be corrupted.");
                Console.WriteLine("Please run the setup utility again:");
                Console.WriteLine("  dotnet run -- --setup");
                Console.ResetColor();
                Log.Error(ex, "Failed to load configuration");
                return 1;
            }

            // Override sender email if --from parameter is provided
            if (!string.IsNullOrWhiteSpace(fromEmail))
            {
                Log.Information("Overriding sender email from '{DefaultSender}' to '{OverrideSender}'", 
                    graphConfig.SenderEmail, fromEmail);
                graphConfig.SenderEmail = fromEmail;
            }

            // Create email service
            var emailService = new EmailService(graphConfig);

            // Send email
            Console.WriteLine("Sending email...");
            await emailService.SendMailAsync(
                to: to,
                cc: cc.Length > 0 ? cc : null,
                bcc: bcc.Length > 0 ? bcc : null,
                subject: subject,
                htmlBodyOrPath: body,
                isBodyAFile: bodyIsFile,
                attachmentPaths: attachments
            );

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Email sent successfully!");
            Console.ResetColor();
            
            Log.Information("=== Email sent successfully ===");
            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            Log.Error(ex, "File not found error: {Message}", ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error sending email: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            Console.ResetColor();
            Log.Error(ex, "Unexpected error: {Message}", ex.Message);
            return 1;
        }
        finally
        {
            Log.Information("=== Mailer Application Ended ===");
            Log.CloseAndFlush();
        }
    }

    static Dictionary<string, string[]> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string[]>();
        var currentKey = "";
        var currentValues = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--") || args[i].StartsWith("-"))
            {
                // Save previous key-value pair
                if (!string.IsNullOrEmpty(currentKey))
                {
                    result[currentKey] = currentValues.ToArray();
                    currentValues.Clear();
                }

                currentKey = args[i];

                // Check if this is a flag (no value)
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--") || args[i + 1].StartsWith("-"))
                {
                    result[currentKey] = Array.Empty<string>();
                    currentKey = "";
                }
            }
            else
            {
                // This is a value for the current key
                currentValues.Add(args[i]);
            }
        }

        // Save last key-value pair
        if (!string.IsNullOrEmpty(currentKey))
        {
            result[currentKey] = currentValues.ToArray();
        }

        return result;
    }

    static void ShowHelp()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        
        Console.WriteLine($"Mailer v{version?.ToString(3) ?? "2.0.0"} - Email Sender using Microsoft Graph API");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  Mailer.exe --to <email> --subject <subject> --body <html> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  --setup              Run configuration setup wizard");
        Console.WriteLine("  --version, -v        Show version information");
        Console.WriteLine("  --help, -h           Show this help message");
        Console.WriteLine();
        Console.WriteLine("Required Arguments:");
        Console.WriteLine("  --to <emails>        Recipient email address(es) (space-separated)");
        Console.WriteLine("  --subject <text>     Email subject");
        Console.WriteLine("  --body <html>        HTML body content");
        Console.WriteLine("  --body-file <path>   Path to HTML file for body content");
        Console.WriteLine();
        Console.WriteLine("Optional Arguments:");
        Console.WriteLine("  --from <email>       Sender email address (overrides database default)");
        Console.WriteLine("  --cc <emails>        CC recipient(s) (space-separated)");
        Console.WriteLine("  --bcc <emails>       BCC recipient(s) (space-separated)");
        Console.WriteLine("  --attachments <paths> File attachment(s) (space-separated)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  Mailer.exe --to user@example.com --subject \"Test\" --body \"<h1>Hello</h1>\"");
        Console.WriteLine("  Mailer.exe --to user1@example.com user2@example.com --subject \"Update\" --body-file email.html");
        Console.WriteLine("  Mailer.exe --to user@example.com --subject \"Files\" --body \"See attached\" --attachments file1.pdf file2.docx");
        Console.WriteLine();
        Console.WriteLine("Configuration:");
        Console.WriteLine("  Run 'Mailer.exe --setup' to configure database and Microsoft Graph API credentials.");
        Console.WriteLine();
    }

    static void ShowVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var infoVersion = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var description = assembly.GetCustomAttribute<System.Reflection.AssemblyDescriptionAttribute>()?.Description;
        var copyright = assembly.GetCustomAttribute<System.Reflection.AssemblyCopyrightAttribute>()?.Copyright;
        
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    Mailer - Email Sender                   ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Version:     {infoVersion ?? version?.ToString() ?? "2.0.0"}");
        Console.WriteLine($"Description: {description ?? "Email sender using Microsoft Graph API"}");
        Console.WriteLine($"Copyright:   {copyright ?? "Copyright © 2025"}");
        Console.WriteLine($"Framework:   .NET {Environment.Version}");
        Console.WriteLine($"Platform:    {Environment.OSVersion}");
        Console.WriteLine();
        Console.WriteLine("Features:");
        Console.WriteLine("  • Microsoft Graph API integration");
        Console.WriteLine("  • SQL Server credential storage");
        Console.WriteLine("  • Serilog logging (console, file, database)");
        Console.WriteLine("  • Automatic retry with exponential backoff");
        Console.WriteLine("  • Performance monitoring");
        Console.WriteLine();
        Console.WriteLine("For help, run: Mailer.exe --help");
        Console.WriteLine();
    }
}
