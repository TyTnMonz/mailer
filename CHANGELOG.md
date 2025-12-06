# Changelog

All notable changes to the Mailer project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [2.0.0] - 2025-12-06

### Added
- **Serilog database logging** - All logs now written to SQL Server `MailerLogs` table
- **Email audit trail** - Every email send is logged with recipients, subject, body, and result
- **Recipients property** - Default recipients can be configured in database (key: "To")
- **Structured logging** - Email events logged with enriched properties for easy querying
- **Documentation organization** - All docs moved to `docs/` folder with comprehensive index
- **Scripts folder** - Build scripts organized in `scripts/` folder
- **Assembly version information** - Version 2.0.0 embedded in executable
- **--version command** - Display version, description, and feature list
- **Automatic retry logic** - Failed email sends automatically retried with exponential backoff (default: 3 retries)
- **Performance monitoring** - Email send duration and success metrics logged to database
- **CHANGELOG.md** - Version history tracking

### Changed
- **Logging architecture** - Switched from custom `EmailLogService` to Serilog with MSSqlServer sink
- **File size** - Self-contained build now ~214 MB (was ~70-80 MB, more accurate measurement)
- **Security documentation** - Updated to reflect SQL Server credential storage (removed outdated DPAPI info)
- **Documentation structure** - Reorganized all documentation into `docs/` folder

### Removed
- **EmailLogService.cs** - Replaced with Serilog
- **Microsoft.Extensions.Configuration** packages - No longer needed (3 packages removed)
- **appsettings.json** reference - Not used
- **Test files** - Removed `test-email.html`, `test-attachment.txt`, `msbuild.log`

### Fixed
- **Build optimization** - Removed unused dependencies
- **Documentation accuracy** - Updated all docs to reflect current implementation

## [1.0.0] - 2025-11-19

### Added
- Initial release
- Email sending via Microsoft Graph API
- OAuth2 client credentials authentication
- Multiple recipients support (To, CC, BCC)
- HTML email body (from string or file)
- File attachments support
- SQL Server credential storage
- Database configuration file (`db.config.json`)
- Self-contained deployment support
- Framework-dependent deployment option
- Comprehensive logging with Serilog (file and console)
- Command-line interface
- Setup utility for configuration
- Security features for `db.config.json` (optional Hidden/System/ReadOnly)

### Security
- Database credentials stored in SQL Server
- Optional file system security for `db.config.json`
- No credentials in source code
- SQL Server authentication support (Windows and SQL)

---

## Version Numbering

This project uses [Semantic Versioning](https://semver.org/):
- **Major**: Breaking changes
- **Minor**: New features (backward compatible)
- **Patch**: Bug fixes (backward compatible)

## Upgrade Notes

### From 1.x to 2.0

**Breaking Changes:**
- None - fully backward compatible

**New Features:**
- Email logging to database is now automatic
- Check `MailerLogs` table for email history
- Query logs using SQL (see `docs/EMAIL_LOGGING.md`)

**Recommendations:**
- Review `docs/EMAIL_LOGGING.md` for privacy considerations
- Implement log retention policy
- Restrict access to `MailerLogs` table

---

**For detailed documentation, see [docs/README.md](docs/README.md)**
