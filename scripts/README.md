# Build Scripts

This folder contains build and publish scripts for the Mailer application.

## Scripts

### publish.bat (Windows)
Publishes the self-contained Windows executable.

**Usage:**
```cmd
scripts\publish.bat
```

**Output:**
- `bin\Release\net9.0\win-x64\publish\Mailer.exe` (214 MB, self-contained)

### publish.sh (Linux/macOS)
Publishes the self-contained executable for Unix-based systems.

**Usage:**
```bash
chmod +x scripts/publish.sh
./scripts/publish.sh
```

## Build Options

### Self-Contained (Default)
Includes .NET runtime, no installation required:
```cmd
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Framework-Dependent
Requires .NET 9 Runtime, smaller file size:
```cmd
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false
```

## See Also

- [DEPLOYMENT.md](../docs/DEPLOYMENT.md) - Full deployment guide
- [DISTRIBUTION.md](../docs/DISTRIBUTION.md) - Distribution package info
