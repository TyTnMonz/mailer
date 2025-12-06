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
Mailer.exe --setup
```

You'll be prompted for:
- **SQL Server IP/Hostname** - e.g., `localhost`, `192.168.1.100`
- **Database Name** - e.g., `MailerDB`, `EmailConfig`
- **Table Name** - e.g., `MailerConfig`, `GraphCredentials`
- **Authentication Method** - Windows Authentication or SQL Server Authentication
- **Azure Tenant ID** - Found in your App Registration → Overview
- **Azure Client ID** - Found in your App Registration → Overview
- **Azure Client Secret** - Created in App Registration → Certificates & secrets
- **Sender Email Address** - A valid email in your Office 365 tenant
- **Default Recipients** (optional) - Default email recipients

The utility will:
- ✅ Test the database connection
- ✅ Create the configuration table if it doesn't exist
- ✅ Store Graph API credentials as key-value pairs in the database
- ✅ Save database connection details to `db.config.json`
- ✅ Optionally apply security attributes to the config file (Hidden, System, ReadOnly)

> [!IMPORTANT]
> **Database Security**: Your Graph API credentials are stored in SQL Server and protected by database authentication. The local `db.config.json` file only contains database connection details.

### Updating Configuration

To update your configuration (e.g., after rotating the client secret or changing database), simply run the setup utility again:

```bash
Mailer.exe --setup
```

## Usage

### Command-Line Options

| Option | Required | Description |
|--------|----------|-------------|
| `--setup` | No | Run the configuration setup wizard |
| `--version`, `-v` | No | Display version information |
| `--help`, `-h` | No | Show help message |
| `--to` | Yes* | Recipient email addresses (space-separated) |
| `--cc` | No | CC recipient email addresses (space-separated) |
| `--bcc` | No | BCC recipient email addresses (space-separated) |
| `--subject` | Yes* | Email subject |
| `--body` | Yes* | HTML body content |
| `--body-file` | Yes* | Path to HTML file for body content |
| `--attachments` | No | File paths for attachments (space-separated) |

\* Either `--body` or `--body-file` is required when sending emails

### Examples

#### Check Version
```bash
Mailer.exe --version
```

#### Basic Email
```bash
Mailer.exe --to recipient@example.com --subject "Test Email" --body "<h1>Hello!</h1><p>This is a test email.</p>"
```

#### Multiple Recipients
```bash
Mailer.exe --to user1@example.com user2@example.com --cc manager@example.com --subject "Team Update" --body "<p>Important update for the team.</p>"
```

#### HTML Body from File
```bash
Mailer.exe --to recipient@example.com --subject "Newsletter" --body-file "templates/newsletter.html"
```

#### With Attachments
```bash
Mailer.exe --to recipient@example.com --subject "Documents" --body "<p>Please find attached documents.</p>" --attachments "file1.pdf" "file2.docx"
```

#### Complete Example
```bash
Mailer.exe --to recipient@example.com --cc manager@example.com --subject "Monthly Report" --body-file "templates/report.html" --attachments "report.pdf" "data.xlsx"
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

// Send email with automatic retry (default: 3 retries)
await emailService.SendMailAsync(
    to: new[] { "recipient@example.com" },
    cc: new[] { "cc@example.com" },
    bcc: null,
    subject: "Test Email",
    htmlBodyOrPath: "<h1>Hello!</h1><p>This is a test.</p>",
    isBodyAFile: false,
    attachmentPaths: null
);

// Custom retry count
await emailService.SendMailAsync(
    to: new[] { "recipient@example.com" },
    subject: "Important",
    htmlBodyOrPath: "<p>Critical message</p>",
    maxRetries: 5  // 6 total attempts
);
```

## Advanced Features

### Automatic Retry Logic

The application automatically retries failed email sends with exponential backoff:

- **Default**: 3 retry attempts (4 total tries)
- **Backoff delays**: 1s, 2s, 4s between retries
- **Configurable**: Adjust `maxRetries` parameter when using as a library
- **Smart logging**: Shows attempt number and retry delays

Example log output:
```
[17:30:15 WRN] ⚠ Attempt 1/4 failed: Network timeout. Retrying in 1000ms...
[17:30:16 INF] ✓ Email sent successfully on attempt 2/4 in 2341ms
```

### Performance Monitoring

Every email send is tracked with performance metrics:

- **Duration**: Milliseconds from start to completion
- **Attempts**: Number of retries needed
- **Success/Failure**: Final result
- **Database logging**: All metrics stored in `MailerLogs` table

Query performance metrics:
```sql
SELECT 
    TimeStamp,
    JSON_VALUE(Properties, '$.DurationMs') AS DurationMs,
    JSON_VALUE(Properties, '$.Attempt') AS Attempts,
    JSON_VALUE(Properties, '$.Success') AS Success
FROM MailerLogs
WHERE Message LIKE '%performance%'
ORDER BY TimeStamp DESC;
```

### Email Audit Trail

All email sends are automatically logged to the `MailerLogs` database table:

- Recipients (To, CC, BCC)
- Subject and body
- Timestamp
- Success/failure status
- Error messages (if failed)

See [docs/EMAIL_LOGGING.md](docs/EMAIL_LOGGING.md) for detailed logging documentation.
See [docs/PERFORMANCE.md](docs/PERFORMANCE.md) for performance monitoring guide.

## Project Structure

```
mailer/
├── Models/
│   ├── DatabaseConfig.cs      # Database connection configuration
│   └── GraphConfig.cs         # Microsoft Graph API configuration
├── Services/
│   ├── EmailService.cs        # Email sending with retry logic
│   └── SecureConfigService.cs # SQL Server credential storage
├── docs/                      # Documentation
│   ├── README.md              # Documentation index
│   ├── DEPLOYMENT.md          # Deployment guide
│   ├── DISTRIBUTION.md        # Self-contained distribution
│   ├── FRAMEWORK_DEPENDENT.md # Framework-dependent version
│   ├── EMAIL_LOGGING.md       # Logging with Serilog
│   ├── PERFORMANCE.md         # Retry logic & monitoring
│   ├── SECURITY.md            # Security guide
│   ├── implementation_plan.md # Implementation plan
│   ├── task.md                # Task tracking
│   └── walkthrough.md         # Development walkthrough
├── scripts/                   # Build scripts
│   ├── publish.bat            # Windows publish script
│   ├── publish.sh             # Unix publish script
│   └── README.md              # Scripts documentation
├── ConfigSetup.cs             # Configuration setup wizard
├── Program.cs                 # Main entry point
├── Mailer.csproj              # Project file
├── README.md                  # This file
├── CHANGELOG.md               # Version history
├── LICENSE                    # License file
└── .gitignore                 # Git ignore rules
```

## Logging

The application uses **Serilog** for comprehensive logging to multiple destinations.

### Log Destinations

- **Console**: Real-time output with timestamps and log levels
- **File**: Daily rotating log files in `logs/` directory (30-day retention)
- **Database**: All logs written to SQL Server `MailerLogs` table

### What Gets Logged

- Application startup and shutdown
- Configuration loading and validation
- Email service initialization
- Email sending operations (recipients, subject, attachments)
- Retry attempts with delays
- Performance metrics (duration, success rate)
- Success confirmations
- All errors with full exception details

### Log File Location

```
logs/
├── mailer-20251206.log
├── mailer-20251205.log
└── ...
```

### Example Log Output

```
[17:30:15 INF] === Mailer Application Started ===
[17:30:15 INF] Starting email send operation. Subject: Test, MaxRetries: 3
[17:30:16 INF] ✓ Email sent successfully on attempt 1/4 in 1234ms
[17:30:16 INF] Email send performance: 1234ms
```

## Documentation

Comprehensive documentation is available in the `docs/` folder:

- **[docs/README.md](docs/README.md)** - Documentation index
- **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)** - Deployment options and guide
- **[docs/SECURITY.md](docs/SECURITY.md)** - Security architecture and best practices
- **[docs/EMAIL_LOGGING.md](docs/EMAIL_LOGGING.md)** - Logging system documentation
- **[docs/PERFORMANCE.md](docs/PERFORMANCE.md)** - Retry logic and performance monitoring
- **[docs/DISTRIBUTION.md](docs/DISTRIBUTION.md)** - Self-contained distribution guide
- **[docs/FRAMEWORK_DEPENDENT.md](docs/FRAMEWORK_DEPENDENT.md)** - Framework-dependent version

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

1. **Natural Language Requirements** - The project was initiated with a simple natural language description
2. **AI-Driven Implementation** - The **Claude Sonnet 4.5** AI model automatically generated:
   - Complete .NET Core project structure
   - All source code with retry logic and performance monitoring
   - NuGet package management and dependencies
   - Comprehensive logging with Serilog (console, file, database)
   - Complete documentation suite
   - Git repository setup and GitHub integration
3. **Iterative Refinement** - Through conversational interactions, features were added:
   - Automatic retry with exponential backoff
   - Performance monitoring and metrics
   - Database logging and audit trail
   - Version information and --version command
4. **Testing and Validation** - The AI assisted in testing with real Azure AD credentials

### Technology Stack

- **IDE**: Google Antigravity
- **AI Model**: Claude Sonnet 4.5
- **Framework**: .NET 9.0
- **APIs**: Microsoft Graph API
- **Authentication**: Azure Identity (OAuth2)
- **Logging**: Serilog (Console, File, MSSqlServer sinks)
- **Database**: Microsoft SQL Server

All code, documentation, and project structure were generated through AI-assisted development.

## Support

- **Documentation**: See [docs/README.md](docs/README.md) for comprehensive guides
- **Microsoft Graph API**: [Official Documentation](https://learn.microsoft.com/en-us/graph/api/user-sendmail)
- **Issues**: Check logs in `logs/` directory and `MailerLogs` database table
- **Version**: Run `Mailer.exe --version` to see current version and features
