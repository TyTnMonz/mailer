# Email Sender Application - Walkthrough

## Overview

Successfully created a .NET Core console application that sends emails using Microsoft Graph API. The application supports both standalone execution via command-line arguments and library usage in other .NET projects.

## Project Structure

```
mailer/
├── Mailer.csproj                  # Project file with NuGet dependencies
├── Program.cs                     # Console app entry point with CLI
├── Services/
│   └── EmailService.cs            # Reusable email service class
├── Models/
│   └── GraphConfig.cs             # Configuration model
├── appsettings.json               # Configuration file (user must configure)
├── appsettings.example.json       # Configuration template
└── README.md                      # Comprehensive documentation
```

## Implemented Components

### 1. GraphConfig Model ([GraphConfig.cs](file:///d:/Antigravity/mailer/Models/GraphConfig.cs))

Configuration model class with properties for:
- `TenantId` - Azure AD tenant ID
- `ClientId` - Application client ID
- `ClientSecret` - Client secret value
- `SenderEmail` - Email address to send from

### 2. EmailService ([EmailService.cs](file:///d:/Antigravity/mailer/Services/EmailService.cs))

Core email service with the following features:

**Authentication:**
- Uses `ClientSecretCredential` from Azure.Identity
- Implements OAuth2 client credentials flow
- Creates authenticated `GraphServiceClient`

**SendMailAsync Method:**
```csharp
public async Task SendMailAsync(
    IEnumerable<string> to,
    IEnumerable<string>? cc = null,
    IEnumerable<string>? bcc = null,
    string subject = "",
    string htmlBodyOrPath = "",
    bool isBodyAFile = false,
    IEnumerable<string>? attachmentPaths = null)
```

**Functionality:**
- ✅ Validates at least one `to` recipient is provided
- ✅ Loads HTML body from file if `isBodyAFile` is true
- ✅ Supports multiple recipients (To, CC, BCC)
- ✅ Handles file attachments with base64 encoding
- ✅ Sends email via Microsoft Graph API

### 3. Console Application ([Program.cs](file:///d:/Antigravity/mailer/Program.cs))

Command-line interface with:
- Manual argument parsing (replaced System.CommandLine due to compatibility)
- Support for all required options
- Help text display
- Configuration loading and validation
- Error handling with colored console output

**Supported Arguments:**
- `--to` (required, multiple) - Recipient emails
- `--cc` (optional, multiple) - CC recipients
- `--bcc` (optional, multiple) - BCC recipients
- `--subject` (required) - Email subject
- `--body` (required) - HTML content or file path
- `--body-is-file` (flag) - Indicates body is a file path
- `--attachments` (optional, multiple) - Attachment file paths
- `--help`, `-h` - Show help message

### 4. Configuration Files

**[appsettings.json](file:///d:/Antigravity/mailer/appsettings.json):**
- Contains placeholder values for Graph API credentials
- Must be configured by user before use
- Automatically copied to output directory on build

**[appsettings.example.json](file:///d:/Antigravity/mailer/appsettings.example.json):**
- Template for users to reference

### 5. Documentation ([README.md](file:///d:/Antigravity/mailer/README.md))

Comprehensive documentation including:
- Prerequisites and Azure AD setup instructions
- Configuration steps
- Usage examples for both console and library modes
- Command-line options reference
- Troubleshooting guide

## NuGet Packages

The project uses the following packages:
- **Microsoft.Graph** (5.97.0) - Microsoft Graph SDK
- **Azure.Identity** (1.17.0) - OAuth2 authentication
- **Microsoft.Extensions.Configuration** (10.0.0) - Configuration framework
- **Microsoft.Extensions.Configuration.Json** (10.0.0) - JSON configuration provider
- **Microsoft.Extensions.Configuration.Binder** (10.0.0) - Configuration binding
- ~~System.CommandLine~~ - Removed due to API compatibility issues

## Build Verification

✅ **Build Status:** Success

The project builds successfully with no errors:
```
Compilazione operazione riuscita in 2,7s
```

Output: `bin\Debug\net9.0\Mailer.dll`

## Usage Examples

### As a Console Application

**Basic email:**
```bash
dotnet run -- --to recipient@example.com --subject "Test Email" --body "<h1>Hello!</h1><p>This is a test.</p>"
```

**Multiple recipients with CC:**
```bash
dotnet run -- --to user1@example.com user2@example.com --cc manager@example.com --subject "Team Update" --body "<p>Important update.</p>"
```

**HTML from file with attachments:**
```bash
dotnet run -- --to recipient@example.com --subject "Report" --body templates/email.html --body-is-file --attachments report.pdf data.xlsx
```

### As a Library

```csharp
using Mailer.Models;
using Mailer.Services;

var config = new GraphConfig
{
    TenantId = "your-tenant-id",
    ClientId = "your-client-id",
    ClientSecret = "your-client-secret",
    SenderEmail = "sender@yourdomain.com"
};

var emailService = new EmailService(config);

await emailService.SendMailAsync(
    to: new[] { "recipient@example.com" },
    subject: "Test Email",
    htmlBodyOrPath: "<h1>Hello!</h1>",
    isBodyAFile: false
);
```

## Key Features Implemented

✅ **Dual Usage:** Works as both standalone executable and library  
✅ **OAuth2 Authentication:** Client credentials flow with Azure AD  
✅ **Multiple Recipients:** Support for To, CC, and BCC  
✅ **HTML Body:** From string or file path  
✅ **Attachments:** Multiple file attachments supported  
✅ **Error Handling:** Comprehensive error messages  
✅ **Configuration:** External JSON configuration file  
✅ **Documentation:** Complete setup and usage guide  

## Next Steps for User

To use the application:

1. **Create Azure AD App Registration:**
   - Register app in Azure Portal
   - Add Mail.Send API permission
   - Grant admin consent
   - Create client secret

2. **Configure appsettings.json:**
   - Copy values from Azure AD app registration
   - Set sender email address

3. **Run the application:**
   - Use command-line for standalone execution
   - Reference project for library usage

## Technical Notes

- **Target Framework:** .NET 9.0
- **Authentication:** OAuth2 client credentials (app-only access)
- **API Permissions Required:** Mail.Send (Application permission)
- **Argument Parsing:** Manual implementation for maximum compatibility
- **Configuration:** Microsoft.Extensions.Configuration framework
