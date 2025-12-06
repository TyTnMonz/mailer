# Mailer Distribution Package

## Quick Start

1. **Copy `Mailer.exe`** to your desired location
2. **Run setup**: `Mailer.exe --setup`
3. **Send emails**: `Mailer.exe --to user@example.com --subject "Test" --body "Hello"`

## System Requirements

- Windows 7 SP1 or later (x64)
- Network access to SQL Server
- No .NET installation required (self-contained)

## First-Time Setup

Run the setup wizard:
```cmd
Mailer.exe --setup
```

You'll be prompted for:
- **Database Server**: IP or hostname (e.g., `192.170.10.13`)
- **Database Name**: e.g., `MailerDB`
- **Table Name**: e.g., `MailerConfig`
- **Authentication**: Windows or SQL Server
- **Azure Tenant ID**: From Azure Portal
- **Azure Client ID**: From App Registration
- **Azure Client Secret**: From App Registration
- **Sender Email**: Your Office 365 email
- **Default Recipients** (optional): Comma-separated emails

## Usage Examples

### Basic Email
```cmd
Mailer.exe --to recipient@example.com --subject "Hello" --body "<h1>Test</h1>"
```

### Multiple Recipients
```cmd
Mailer.exe --to user1@example.com user2@example.com --subject "Update" --body "Message"
```

### With Attachments
```cmd
Mailer.exe --to user@example.com --subject "Files" --body "See attached" --attachments file1.pdf file2.docx
```

### HTML from File
```cmd
Mailer.exe --to user@example.com --subject "Newsletter" --body-file newsletter.html
```

## Configuration Files

After setup, you'll have:
- **`db.config.json`**: Database connection details (in same folder as exe)
- **Database table**: Graph API credentials (in SQL Server)

## Deployment Options

### Option 1: Network Share
- Place `Mailer.exe` on a network share
- All users run from `\\server\share\Mailer.exe`
- Run setup once to create shared config

### Option 2: Local Installation (Recommended)
- Copy `Mailer.exe` to each PC (e.g., `C:\Program Files\Mailer\`)
- Each user runs setup individually
- Better performance, no network dependency

## Troubleshooting

**"Database connection not configured"**
- Run `Mailer.exe --setup`

**"Configuration not found in database"**
- Run setup again to save credentials

**"Access denied" errors**
- Ensure write permissions to exe folder
- Try running as administrator for first setup

## Security

✅ Self-contained (no .NET installation needed)
✅ Database credentials protected by SQL Server
✅ Config file can be secured (Hidden/ReadOnly)
✅ No credentials in source code

## File Size

~215 MB (includes .NET 9 runtime)

## Support

For issues, check:
- SQL Server is accessible
- Database credentials are correct
- Azure AD app has Mail.Send permission
- Sender email exists in Office 365

---

**Version**: 1.0
**Build Date**: 2025-12-02
