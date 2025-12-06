# Deployment Guide

## Self-Contained Deployment

The Mailer application is configured for **self-contained deployment**, which means it includes the .NET runtime and doesn't require users to install .NET separately.

## Publishing the Application

### Windows (Recommended)

Run the included publish script:

```cmd
publish.bat
```

This will create a single executable at:
```
bin\Release\net9.0\win-x64\publish\Mailer.exe
```

### Manual Publishing

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Deployment Options

### Option 1: Network Share Deployment

**Pros:**
- Single copy of the application
- Easy to update (update once, affects all users)
- Centralized configuration

**Cons:**
- Requires network connectivity
- Potential file locking issues with logs
- Users need read access to the share

**Steps:**
1. Publish the application using `publish.bat`
2. Copy the entire `bin\Release\net9.0\win-x64\publish\` folder to a network share
3. Run setup once: `\\server\share\Mailer.exe --setup`
4. Users can run: `\\server\share\Mailer.exe --to user@example.com --subject "Test" --body "Hello"`

**Important Notes:**
- The `db.config.json` file will be created in the shared folder
- All users will share the same database configuration
- Logs will be written to `logs\` in the shared folder (may cause conflicts)

### Option 2: Local Installation (Recommended)

**Pros:**
- No network dependency
- Each user can have their own configuration
- No file locking issues
- Faster execution

**Cons:**
- Must copy to each PC
- Updates require re-copying to all PCs

**Steps:**
1. Publish the application using `publish.bat`
2. Copy `Mailer.exe` from `bin\Release\net9.0\win-x64\publish\` to each user's PC
3. Each user runs setup: `Mailer.exe --setup`
4. Users can run: `Mailer.exe --to user@example.com --subject "Test" --body "Hello"`

**Recommended Location:**
- `C:\Program Files\Mailer\Mailer.exe` (requires admin to copy)
- `C:\Users\<username>\AppData\Local\Mailer\Mailer.exe` (no admin required)

### Option 3: Hybrid Approach

**Best of both worlds:**
1. Store the executable on a network share
2. Store configuration (`db.config.json`) locally on each PC
3. All users connect to the same SQL Server database

**Implementation:**
- Copy `Mailer.exe` to network share
- Modify code to store `db.config.json` in user's AppData folder (requires code change)

## File Sizes

- **Regular build**: ~200 KB (requires .NET 9 Runtime)
- **Self-contained build**: ~70-80 MB (includes .NET runtime)
- **Trimmed build**: ~60-70 MB (with PublishTrimmed enabled)

## System Requirements

- **Operating System**: Windows 7 SP1 or later (x64)
- **Architecture**: 64-bit (x64)
- **Disk Space**: ~100 MB (including logs)
- **Network**: Access to SQL Server
- **Permissions**: Read/Write access to application folder (for logs and config)

## Configuration

After deployment, each installation needs to be configured:

```cmd
Mailer.exe --setup
```

This will prompt for:
- Database server IP
- Database name
- Table name
- Authentication method
- Microsoft Graph API credentials

## Updating the Application

### Network Share
1. Publish new version
2. Replace files in the network share
3. Users automatically get the new version on next run

### Local Installation
1. Publish new version
2. Copy new `Mailer.exe` to each PC
3. Configuration is preserved (stored in `db.config.json`)

## Troubleshooting

### "Application failed to start"
- Ensure you're using the self-contained build from `publish\` folder
- Check that the file isn't blocked (Right-click → Properties → Unblock)

### "Access denied" errors
- Ensure users have write permissions to the application folder
- Logs and `db.config.json` need to be writable

### Network share performance issues
- Consider local installation instead
- Use SSD storage for the network share
- Ensure good network connectivity

## Security Considerations

- `db.config.json` contains database connection details (marked as Hidden, System, ReadOnly)
- Graph API credentials are stored in SQL Server database
- Ensure proper SQL Server permissions
- Use Windows Authentication when possible
- Restrict access to the application folder
