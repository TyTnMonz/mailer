# Mailer - Email Sender with Microsoft Graph API

> **🤖 AI-Generated Project**  
> This entire project, including all code, documentation, and this GitHub repository, was created automatically using **Google's Antigravity IDE** with the **Claude Sonnet 4.5** AI model as a personal test of the IDE's AI-powered development capabilities.

A .NET Core console application that sends emails using the Microsoft Graph API (Office 365). This application can be used both as a standalone executable and as a library in other .NET projects.

## Features

- ✉️ Send emails via Microsoft Graph API
- 🔐 OAuth2 client credentials authentication
- 👥 Support for multiple recipients (To, CC, BCC)
- 📝 HTML email body (from string or file)
- 📎 File attachments support
- 📊 Comprehensive logging with daily rotating log files
- 🔧 Reusable `EmailService` class for library usage
- 💻 Command-line interface for standalone execution

## Prerequisites

### Azure AD App Registration

Before using this application, you must create an Azure AD App Registration:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations** → **New registration**
3. Enter a name (e.g., "Email Sender App")
4. Select **Accounts in this organizational directory only**
5. Click **Register**

### Configure API Permissions

1. In your app registration, go to **API permissions**
2. Click **Add a permission** → **Microsoft Graph** → **Application permissions**
3. Search for and add: **Mail.Send**
4. Click **Grant admin consent** (requires admin privileges)

### Create Client Secret

1. Go to **Certificates & secrets** → **New client secret**
2. Add a description and select an expiration period
3. Click **Add**
4. **Copy the secret value immediately** (you won't be able to see it again)

### Gather Required Information

You'll need the following values from your app registration:
- **Application (client) ID** - Found on the Overview page
- **Directory (tenant) ID** - Found on the Overview page
- **Client secret value** - Created in the previous step
- **Sender email address** - A valid email in your Office 365 tenant

## Configuration

1. Copy `appsettings.example.json` to `appsettings.json`:
   ```bash
   cp appsettings.example.json appsettings.json
   ```

2. Edit `appsettings.json` with your Azure AD credentials:
   ```json
   {
     "GraphConfig": {
       "TenantId": "your-tenant-id-here",
       "ClientId": "your-client-id-here",
       "ClientSecret": "your-client-secret-here",
       "SenderEmail": "sender@yourdomain.com"
     }
   }
   ```

## Usage

### As a Console Application

Build the project:
```bash
dotnet build
```

Run the application with command-line arguments:

#### Basic Example
```bash
dotnet run -- --to recipient@example.com --subject "Test Email" --body "<h1>Hello!</h1><p>This is a test email.</p>"
```

#### Multiple Recipients
```bash
dotnet run -- --to recipient1@example.com recipient2@example.com --cc cc@example.com --bcc bcc@example.com --subject "Team Update" --body "<p>Important update for the team.</p>"
```

#### HTML Body from File
```bash
dotnet run -- --to recipient@example.com --subject "Newsletter" --body "path/to/email.html" --body-is-file
```

#### With Attachments
```bash
dotnet run -- --to recipient@example.com --subject "Documents" --body "<p>Please find attached documents.</p>" --attachments "file1.pdf" "file2.docx"
```

#### Complete Example
```bash
dotnet run -- --to recipient@example.com --cc manager@example.com --subject "Monthly Report" --body "templates/report.html" --body-is-file --attachments "report.pdf" "data.xlsx"
```

### As a Library

Reference this project in your .NET application and use the `EmailService` class:

```csharp
using Mailer.Models;
using Mailer.Services;

// Configure Graph API settings
var config = new GraphConfig
{
    TenantId = "your-tenant-id",
    ClientId = "your-client-id",
    ClientSecret = "your-client-secret",
    SenderEmail = "sender@yourdomain.com"
};

// Create email service
var emailService = new EmailService(config);

// Send email with HTML string
await emailService.SendMailAsync(
    to: new[] { "recipient@example.com" },
    cc: new[] { "cc@example.com" },
    bcc: null,
    subject: "Test Email",
    htmlBodyOrPath: "<h1>Hello!</h1><p>This is a test.</p>",
    isBodyAFile: false,
    attachmentPaths: null
);

// Send email with HTML file and attachments
await emailService.SendMailAsync(
    to: new[] { "recipient@example.com" },
    cc: null,
    bcc: null,
    subject: "Report",
    htmlBodyOrPath: "templates/email.html",
    isBodyAFile: true,
    attachmentPaths: new[] { "report.pdf", "data.xlsx" }
);
```

## Command-Line Options

| Option | Required | Description |
|--------|----------|-------------|
| `--to` | Yes | Recipient email addresses (can specify multiple) |
| `--cc` | No | CC recipient email addresses (can specify multiple) |
| `--bcc` | No | BCC recipient email addresses (can specify multiple) |
| `--subject` | Yes | Email subject |
| `--body` | Yes | HTML body content or path to HTML file |
| `--body-is-file` | No | Indicates if `--body` is a file path (default: false) |
| `--attachments` | No | File paths for attachments (can specify multiple) |

## Project Structure

```
mailer/
├── Mailer.csproj              # Project file
├── Program.cs                 # Console app entry point
├── Services/
│   └── EmailService.cs        # Reusable email service
├── Models/
│   └── GraphConfig.cs         # Configuration model
├── docs/                      # AI development artifacts
│   ├── implementation_plan.md # Initial planning document
│   ├── task.md                # Task breakdown and progress
│   └── walkthrough.md         # Project walkthrough
├── appsettings.json           # Configuration file (user-created)
├── appsettings.example.json   # Configuration template
└── README.md                  # This file
```

## Logging

The application uses **Serilog** for comprehensive logging of all operations.

### Log Configuration

- **Console Output:** Logs are displayed in the console with timestamps and log levels
- **File Output:** Daily rotating log files in the `logs/` directory
- **Log File Pattern:** `logs/mailer-YYYYMMDD.log`
- **Retention:** Keeps the last 30 days of log files
- **Log Levels:** Debug, Information, Warning, Error

### What Gets Logged

- Application startup and shutdown
- Configuration loading and validation
- Email service initialization
- Email sending operations (recipients, subject, attachments)
- File operations (HTML body loading, attachment processing)
- Success confirmations
- All errors with full exception details

### Log File Location

Log files are created in the `logs/` directory:
```
logs/
├── mailer-20251119.log
├── mailer-20251120.log
└── ...
```

### Example Log Output

```
2025-11-19 21:12:12.089 +01:00 [INF] === Mailer Application Started ===
2025-11-19 21:12:12.150 +01:00 [INF] Command line arguments: --to user@example.com
2025-11-19 21:12:12.165 +01:00 [INF] Loading configuration from appsettings.json
2025-11-19 21:12:12.229 +01:00 [INF] Configuration loaded successfully
2025-11-19 21:12:12.271 +01:00 [INF] Initializing EmailService
2025-11-19 21:12:12.375 +01:00 [INF] Starting email send operation
2025-11-19 21:12:13.677 +01:00 [INF] Email sent successfully
2025-11-19 21:12:13.680 +01:00 [INF] === Mailer Application Ended ===
```

## Troubleshooting

### "Insufficient privileges to complete the operation"
- Ensure **Mail.Send** permission is granted and admin consent is provided
- Verify the sender email exists in your Office 365 tenant

### "Configuration is incomplete"
- Check that all fields in `appsettings.json` are filled correctly
- Ensure the file is copied to the output directory (should happen automatically)

### "Authentication failed"
- Verify your Client ID, Tenant ID, and Client Secret are correct
- Check that the client secret hasn't expired

### File not found errors
- Ensure file paths for HTML body and attachments are correct
- Use absolute paths or paths relative to the executable location

## License

This project is licensed under the **Creative Commons Attribution-NonCommercial 4.0 International License (CC BY-NC 4.0)**.

**You are free to:**
- Share and redistribute the material
- Adapt, remix, and build upon the material

**Under the following terms:**
- **Attribution** — You must give appropriate credit
- **NonCommercial** — You may not use the material for commercial purposes

For commercial use, please contact the author.

See the [LICENSE](LICENSE) file for full details or visit [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/).

## About This AI-Generated Project

### Development Process

This entire project was created using **Google's Antigravity IDE**, a next-generation development environment powered by advanced AI capabilities. The development process involved:

1. **Natural Language Requirements** - The project was initiated with a simple natural language description of the desired functionality
2. **AI-Driven Implementation** - The **Claude Sonnet 4.5** AI model automatically generated:
   - Complete .NET Core project structure
   - All source code (EmailService, Program.cs, configuration models)
   - NuGet package management and dependencies
   - Comprehensive logging with Serilog
   - Complete documentation (README, LICENSE)
   - Git repository setup and GitHub integration
3. **Iterative Refinement** - Through conversational interactions, features were added and refined:
   - HTML email support (string and file-based)
   - File attachment handling
   - Daily rotating log files
   - Command-line interface
4. **Testing and Validation** - The AI assisted in testing the application with real Azure AD credentials and email sending

### What Was Tested

This project tested Antigravity IDE's ability to:
- Generate production-ready, well-structured code
- Implement complex integrations (Microsoft Graph API, OAuth2)
- Create comprehensive documentation
- Handle real-world requirements (logging, error handling, configuration)
- Manage the entire development lifecycle from concept to GitHub deployment

### Technology Stack

- **IDE**: Google Antigravity
- **AI Model**: Claude Sonnet 4.5
- **Framework**: .NET 9.0
- **APIs**: Microsoft Graph API
- **Authentication**: Azure Identity (OAuth2)
- **Logging**: Serilog

All code, documentation, and project structure were generated through AI-assisted development, demonstrating the potential of AI-powered IDEs to accelerate software development while maintaining code quality and best practices.

## Support

For issues related to Microsoft Graph API, consult the [official documentation](https://learn.microsoft.com/en-us/graph/api/user-sendmail).
