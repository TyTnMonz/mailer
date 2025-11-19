# .NET Core Email Sender with Microsoft Graph API

This implementation plan outlines the creation of a .NET Core console application that sends emails using the Microsoft Graph API. The application will support both standalone execution via command-line arguments and library usage through a reusable `EmailService` class.

## User Review Required

> [!IMPORTANT]
> **Azure App Registration Required**
> 
> Before using this application, you must create an Azure AD App Registration with the following:
> - **Application (client) ID**
> - **Directory (tenant) ID**
> - **Client Secret**
> - **API Permissions**: `Mail.Send` (Application permission, requires admin consent)
> 
> You'll need to configure these values in `appsettings.json` after the project is created.

> [!WARNING]
> **Sender Email Address**
> 
> The application will send emails on behalf of a specific user or shared mailbox. You must specify the sender's email address in the configuration. This email must exist in your Office 365 tenant.

## Proposed Changes

### Project Structure

The application will be organized as follows:

```
mailer/
├── Mailer.csproj
├── Program.cs                 # Console app entry point with CLI argument parsing
├── Services/
│   └── EmailService.cs        # Reusable email service with SendMailAsync method
├── Models/
│   └── GraphConfig.cs         # Configuration model for Graph API settings
├── appsettings.json           # Configuration file (with placeholder values)
├── appsettings.example.json   # Example configuration template
└── README.md                  # Setup and usage instructions
```

---

### Core Components

#### [NEW] [Mailer.csproj](file:///d:/Antigravity/mailer/Mailer.csproj)

.NET Core console application project file with required dependencies:
- **Microsoft.Graph** - Microsoft Graph SDK
- **Azure.Identity** - OAuth2 authentication with client credentials
- **Microsoft.Extensions.Configuration** - Configuration management
- **Microsoft.Extensions.Configuration.Json** - JSON configuration provider
- **System.CommandLine** - Modern command-line argument parsing

Target framework: .NET 6.0 or later (will use .NET 8.0 for latest features)

---

#### [NEW] [Models/GraphConfig.cs](file:///d:/Antigravity/mailer/Models/GraphConfig.cs)

Configuration model class to hold Microsoft Graph API settings:
- `TenantId` - Azure AD tenant ID
- `ClientId` - Application (client) ID
- `ClientSecret` - Client secret value
- `SenderEmail` - Email address to send from

---

#### [NEW] [Services/EmailService.cs](file:///d:/Antigravity/mailer/Services/EmailService.cs)

Core email service class with the following features:

**Authentication:**
- Uses `ClientSecretCredential` from Azure.Identity
- Implements OAuth2 client credentials flow
- Creates authenticated `GraphServiceClient`

**Public Method:**
```csharp
public async Task SendMailAsync(
    IEnumerable<string> to,
    IEnumerable<string>? cc,
    IEnumerable<string>? bcc,
    string subject,
    string htmlBodyOrPath,
    bool isBodyAFile,
    IEnumerable<string>? attachmentPaths = null)
```

**Functionality:**
- Validates recipient lists (at least one `to` recipient required)
- Loads HTML body from file if `isBodyAFile` is true, otherwise uses the string directly
- Builds `Message` object with recipients, subject, and HTML body
- Processes attachments by reading files and converting to base64
- Sends email using `GraphServiceClient.Users[senderEmail].SendMail`

---

#### [NEW] [Program.cs](file:///d:/Antigravity/mailer/Program.cs)

Console application entry point with command-line interface:

**Command-line Arguments:**
- `--to` (required, multiple) - Recipient email addresses
- `--cc` (optional, multiple) - CC recipients
- `--bcc` (optional, multiple) - BCC recipients
- `--subject` (required) - Email subject
- `--body` (required) - HTML body content or file path
- `--body-is-file` (optional, flag) - Indicates if body is a file path
- `--attachments` (optional, multiple) - File paths for attachments

**Functionality:**
- Uses `System.CommandLine` for robust argument parsing
- Loads configuration from `appsettings.json`
- Creates `EmailService` instance
- Calls `SendMailAsync` with parsed arguments
- Provides clear error messages and exit codes

---

#### [NEW] [appsettings.json](file:///d:/Antigravity/mailer/appsettings.json)

Configuration file with placeholder values:
```json
{
  "GraphConfig": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "SenderEmail": "sender@yourdomain.com"
  }
}
```

File will be copied to output directory on build.

---

#### [NEW] [appsettings.example.json](file:///d:/Antigravity/mailer/appsettings.example.json)

Example configuration template with instructions for users to copy and fill in their own values.

---

#### [NEW] [README.md](file:///d:/Antigravity/mailer/README.md)

Comprehensive documentation including:
- **Prerequisites** - Azure AD app registration steps
- **Configuration** - How to set up `appsettings.json`
- **Usage as Console App** - Command-line examples
- **Usage as Library** - How to reference and use `EmailService` in other projects
- **Examples** - Common scenarios (simple email, with attachments, HTML file body, etc.)

---

## Verification Plan

### Automated Tests
- Build the project: `dotnet build`
- Verify no compilation errors
- Check that all NuGet packages restore correctly

### Manual Verification
Since this requires Azure AD credentials and a real Office 365 tenant:
- Provide clear instructions in README for user to configure their own credentials
- User will need to test with their own Azure AD app registration
- Suggest testing scenarios:
  1. Simple email with HTML body string
  2. Email with HTML body from file
  3. Email with multiple recipients (to, cc, bcc)
  4. Email with attachments
  5. Using the library in another .NET project
