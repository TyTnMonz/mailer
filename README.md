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
- 🔄 **Automatic retry with exponential backoff** (default: 3 retries)
- ⚡ **Performance monitoring** - Email send duration tracked
- 📊 Comprehensive logging with Serilog (console, file, database)
- 🗄️ **Email audit trail** - All sends logged to SQL Server
- 🔧 Reusable `EmailService` class for library usage
- 💻 Command-line interface for standalone execution
- 📦 Self-contained deployment (no .NET installation required)

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

### SQL Server

- Microsoft SQL Server (local or remote)
- Windows Authentication or SQL Server authentication
- Permissions to create tables and read/write data

## Deployment

This application is configured for **self-contained deployment** - it includes the .NET runtime and doesn't require users to install .NET separately.

### Publishing the Application

Run the included publish script:

```cmd
scripts\publish.bat
```

Or manually:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output: `bin\Release\net9.0\win-x64\publish\Mailer.exe` (~214 MB)

### Deployment Options

**Option 1: Network Share**
- Copy `Mailer.exe` to a shared folder
- All users run from `\\server\share\Mailer.exe`
- Shared configuration and logs

**Option 2: Local Installation (Recommended)**
- Copy `Mailer.exe` to each user's PC
- Independent configuration per user
- Better performance, no network dependency

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for detailed deployment instructions.

## Configuration

### Database Setup

This application uses **SQL Server** to store Microsoft Graph API credentials securely. Database connection details are stored in a local configuration file (`db.config.json`) in the program folder.

**Run the setup utility to configure the application:**

```bash
dotnet run -- --setup
```

The setup utility will prompt you for:

#### Database Connection
- **Server IP/Hostname** - e.g., `localhost`, `192.168.1.100`
- **Database Name** - e.g., `MailerDB`, `SaccoGestT`
- **Table Name** - e.g., `SaccoGest`, `MailerConfig`
- **Authentication Method** - Windows Authentication or SQL Server login
- **SQL Credentials** - Username and password (if using SQL Server authentication)

#### Microsoft Graph API Configuration
- **Azure Tenant ID** - Found in Azure Portal → Azure Active Directory → Overview
- **Azure Client ID** - Found in your App Registration → Overview
- **Azure Client Secret** - Created in App Registration → Certificates & secrets
- **Sender Email Address** - A valid email in your Office 365 tenant

The utility will:
- ✅ Test the database connection
- ✅ Create the configuration table if it doesn't exist
- ✅ Store Graph API credentials as key-value pairs in the database
- ✅ Save database connection details to `db.config.json`
- ✅ Apply security attributes to the config file (Hidden, System, ReadOnly)

> [!IMPORTANT]
> **Database Security**: Your Graph API credentials are stored in SQL Server and protected by database authentication. The local `db.config.json` file only contains database connection details.

> [!WARNING]
> **File Protection**: The `db.config.json` file is marked as Hidden, System, and ReadOnly. The application handles this automatically during setup and runtime.

### Updating Configuration

To update your configuration (e.g., after rotating the client secret or changing database), simply run the setup utility again:

```bash
dotnet run -- --setup
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
| `--setup` | No | Run the secure configuration setup utility |
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
├── ConfigSetup.cs             # Configuration setup utility
├── Services/
│   ├── EmailService.cs        # Reusable email service
│   └── SecureConfigService.cs # SQL Server configuration management
├── Models/
│   ├── GraphConfig.cs         # Graph API configuration model
│   └── DatabaseConfig.cs      # Database connection configuration
├── docs/                      # AI development artifacts
│   ├── implementation_plan.md # Initial planning document
│   ├── task.md                # Task breakdown and progress
│   └── walkthrough.md         # Project walkthrough
├── db.config.json             # Database connection config (created by setup, hidden/readonly)
├── SECURITY.md                # Security documentation
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

All code, documentation, and project structure were generated through AI-assisted development.

## Support

For issues related to Microsoft Graph API, consult the [official documentation](https://learn.microsoft.com/en-us/graph/api/user-sendmail).
