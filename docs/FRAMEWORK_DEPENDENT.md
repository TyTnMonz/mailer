# Mailer - Framework-Dependent Version

## Requirements

**IMPORTANT**: This version requires .NET 9 Runtime to be installed on the target PC.

### Download .NET 9 Runtime
- **Direct Link**: https://dotnet.microsoft.com/download/dotnet/9.0
- **File**: .NET Desktop Runtime 9.0.x (x64)
- **Size**: ~55 MB download

### System Requirements
- Windows 7 SP1 or later (x64)
- .NET 9 Runtime (Desktop)
- Network access to SQL Server

## Advantages

✅ **Much smaller file size** (~200 KB vs ~215 MB)
✅ **Faster startup** (no extraction needed)
✅ **Easier to update** (just replace small exe)
✅ **Shared runtime** (one installation for all .NET 9 apps)

## Disadvantages

❌ Requires .NET 9 Runtime installation
❌ Requires admin rights to install runtime
❌ Users must install runtime first

## Installation

### For Network Administrators

Deploy .NET 9 Runtime via:
- **Group Policy (GPO)**
- **SCCM/MECM**
- **Microsoft Intune**
- **PowerShell scripts**

**Silent Installation:**
```cmd
dotnet-desktop-runtime-9.0.x-win-x64.exe /install /quiet /norestart
```

### For End Users

1. **Install .NET 9 Runtime** (one-time, requires admin)
   - Download from https://dotnet.microsoft.com/download/dotnet/9.0
   - Run installer
   - Restart may be required

2. **Copy `Mailer.exe`** to desired location

3. **Run setup**: `Mailer.exe --setup`

4. **Start using**: `Mailer.exe --to user@example.com --subject "Test" --body "Hello"`

## Usage

Same as self-contained version - see main README for usage examples.

## When to Use This Version

**Use Framework-Dependent if:**
- ✅ .NET 9 Runtime is already deployed in your organization
- ✅ Network admin can deploy runtime via GPO/SCCM
- ✅ You want smaller file size
- ✅ You update the app frequently

**Use Self-Contained if:**
- ✅ Users don't have admin rights
- ✅ .NET runtime cannot be deployed centrally
- ✅ You want zero dependencies
- ✅ Simplicity is more important than file size

## File Comparison

| Version | File Size | .NET Required | Deployment |
|---------|-----------|---------------|------------|
| Self-Contained | ~215 MB | No | Copy & Run |
| Framework-Dependent | ~200 KB | Yes (.NET 9) | Install Runtime + Copy |

---

**Version**: 1.0 (Framework-Dependent)
**Build Date**: 2025-12-02
