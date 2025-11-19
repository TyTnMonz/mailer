using Microsoft.Extensions.Configuration;
using Mailer.Models;
using Mailer.Services;
using Serilog;

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

            Log.Information("Email parameters - To: {To}, Subject: {Subject}, BodyIsFile: {BodyIsFile}, Attachments: {AttachmentCount}",
                string.Join(", ", to), subject, bodyIsFile, attachments?.Length ?? 0);

            // Load configuration
            Log.Information("Loading configuration from appsettings.json");
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var graphConfig = new GraphConfig();
            configuration.GetSection("GraphConfig").Bind(graphConfig);

            // Validate configuration
            if (string.IsNullOrWhiteSpace(graphConfig.TenantId) ||
                string.IsNullOrWhiteSpace(graphConfig.ClientId) ||
                string.IsNullOrWhiteSpace(graphConfig.ClientSecret) ||
                string.IsNullOrWhiteSpace(graphConfig.SenderEmail))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Microsoft Graph configuration is incomplete.");
                Console.WriteLine("Please configure TenantId, ClientId, ClientSecret, and SenderEmail in appsettings.json");
                Console.ResetColor();
                Log.Error("Microsoft Graph configuration is incomplete in appsettings.json");
                return 1;
            }

            Log.Information("Configuration loaded successfully. Sender: {SenderEmail}", graphConfig.SenderEmail);

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
        Console.WriteLine("Mailer - Send emails using Microsoft Graph API");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  Mailer --to <email> [--cc <email>] [--bcc <email>] --subject <subject> --body <html|path> [--body-is-file] [--attachments <file>...]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --to <email>           Recipient email addresses (required, can specify multiple)");
        Console.WriteLine("  --cc <email>           CC recipient email addresses (optional, can specify multiple)");
        Console.WriteLine("  --bcc <email>          BCC recipient email addresses (optional, can specify multiple)");
        Console.WriteLine("  --subject <subject>    Email subject (required)");
        Console.WriteLine("  --body <html|path>     HTML body content or path to HTML file (required)");
        Console.WriteLine("  --body-is-file         Indicates if --body is a file path (default: false)");
        Console.WriteLine("  --attachments <file>   File paths for attachments (optional, can specify multiple)");
        Console.WriteLine("  --help, -h             Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  Mailer --to recipient@example.com --subject \"Test\" --body \"<h1>Hello!</h1>\"");
        Console.WriteLine("  Mailer --to user@example.com --cc manager@example.com --subject \"Report\" --body report.html --body-is-file --attachments file1.pdf file2.xlsx");
    }
}
