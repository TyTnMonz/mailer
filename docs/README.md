# Mailer Documentation

Complete documentation for the Mailer email sending application.

## 📚 Documentation Index

### Getting Started
- **[Main README](../README.md)** - Project overview, features, and quick start guide
- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Deployment options and installation guide

### Configuration & Security
- **[SECURITY.md](SECURITY.md)** - Security architecture, best practices, and privacy considerations
- **[EMAIL_LOGGING.md](EMAIL_LOGGING.md)** - Email logging with Serilog (database, file, console)
- **[PERFORMANCE.md](PERFORMANCE.md)** - Retry logic, performance monitoring, and metrics

### Distribution
- **[DISTRIBUTION.md](DISTRIBUTION.md)** - Self-contained distribution guide
- **[FRAMEWORK_DEPENDENT.md](FRAMEWORK_DEPENDENT.md)** - Framework-dependent version guide

### Development
- **[implementation_plan.md](implementation_plan.md)** - Original implementation plan
- **[walkthrough.md](walkthrough.md)** - Development walkthrough and testing
- **[task.md](task.md)** - Task breakdown and progress tracking

## 🚀 Quick Links

### For End Users
1. [How to install](DEPLOYMENT.md#deployment-options)
2. [How to configure](../README.md#configuration)
3. [How to send emails](../README.md#usage)
4. [Troubleshooting](DEPLOYMENT.md#troubleshooting)

### For Administrators
1. [Deployment options](DEPLOYMENT.md)
2. [Security setup](SECURITY.md#best-practices)
3. [Database logging](EMAIL_LOGGING.md)
4. [Distribution packages](DISTRIBUTION.md)

### For Developers
1. [Project structure](../README.md#project-structure)
2. [Building from source](../README.md#building-from-source)
3. [Using as a library](../README.md#using-as-a-library)
4. [Implementation details](implementation_plan.md)

## 📋 Document Summaries

### DEPLOYMENT.md
Comprehensive deployment guide covering:
- Self-contained vs framework-dependent builds
- Network share vs local installation
- Configuration and setup
- Troubleshooting deployment issues

### SECURITY.md
Security documentation including:
- SQL Server credential storage
- Database configuration file security
- Email logging privacy considerations
- Production security checklist

### EMAIL_LOGGING.md
Logging system documentation:
- Serilog configuration (console, file, database)
- MailerLogs table structure
- Example SQL queries
- Privacy and retention policies

### PERFORMANCE.md
Performance monitoring and retry logic:
- Automatic retry with exponential backoff
- Performance metrics tracking
- Duration and success rate monitoring
- Example queries for analytics

### DISTRIBUTION.md
Distribution package guide:
- Self-contained version (214 MB, no .NET required)
- Quick start instructions
- Usage examples
- System requirements

### FRAMEWORK_DEPENDENT.md
Framework-dependent version guide:
- Smaller file size (53 MB)
- .NET 9 Runtime requirement
- Installation instructions
- When to use this version

## 🔧 Configuration Files

The application uses these configuration files:

| File | Location | Purpose | Security |
|------|----------|---------|----------|
| `db.config.json` | Program directory | Database connection details | Optional (Hidden/System/ReadOnly) |
| `MailerLogs` | SQL Server | Email send logs | SQL Server permissions |
| `SaccoGest` | SQL Server | Graph API credentials | SQL Server permissions |
| `logs/*.log` | Program directory | Application logs | File system permissions |

## 📊 Architecture Overview

```
┌─────────────────┐
│  Mailer.exe     │
│  (CLI App)      │
└────────┬────────┘
         │
         ├─────────────────────────────────────┐
         │                                     │
         ▼                                     ▼
┌─────────────────┐                  ┌─────────────────┐
│  EmailService   │                  │   Serilog       │
│  (Graph API)    │                  │   (Logging)     │
└────────┬────────┘                  └────────┬────────┘
         │                                     │
         ▼                                     ▼
┌─────────────────┐                  ┌─────────────────┐
│  SQL Server     │◄─────────────────│  MailerLogs     │
│  (Credentials)  │                  │  (Email Logs)   │
└─────────────────┘                  └─────────────────┘
```

## 🆘 Support

For issues or questions:

1. Check [Troubleshooting](DEPLOYMENT.md#troubleshooting)
2. Review [Security Checklist](SECURITY.md#security-checklist)
3. Check application logs in `logs/` directory
4. Query `MailerLogs` table for email send history

## 📝 Version History

- **v2.0** - Serilog-based logging with database sink
- **v1.0** - Initial release with SQL Server credential storage

---

**Last Updated**: 2025-12-06
