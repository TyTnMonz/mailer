# Logging Configuration

## Overview

The Mailer application uses **Serilog** for comprehensive logging with different log levels configured for each destination (sink). This provides clean console output while maintaining detailed logs in the database for troubleshooting and analysis.

## Log Levels by Sink

All log levels are **configurable during setup** (`--setup`).

### Console Output
- **Level**: Information (fixed)
- **Purpose**: Clean, production-ready output
- **Contains**: Important events only
  - Application start/end
  - Email initialization
  - Email sent successfully
  - Errors and warnings

### File Logs (`logs/mailer-YYYYMMDD.log`)
- **Level**: Configurable (default: Information)
- **Options**: Debug, Information, Warning, Error
- **Purpose**: File-based review and troubleshooting
- **Retention**: 30 days (automatic cleanup)
- **Format**: Timestamped with timezone
- **Can be disabled**: Yes

### Database (`MailerLogs` table)
- **Level**: Configurable (default: Debug)
- **Options**: Debug, Information, Warning, Error
- **Purpose**: Complete application event log for querying
- **Contains**: All events at configured level and above
- **Can be disabled**: Yes

### Email History (`EmailHistory` table)
- **Level**: N/A (dedicated table)
- **Purpose**: Clean email audit trail
- **Contains**: One row per email with all details
- **Can be disabled**: Yes
- **See**: [EMAIL_HISTORY.md](EMAIL_HISTORY.md)

## Configuration During Setup

When you run `--setup`, you'll be asked:

**Step 3: Logging Configuration**
1. Enable file logging? (y/n, default: y)
   - If yes: File log level? (D=Debug, I=Information, W=Warning, E=Error, default: I)
2. Enable database full logs (MailerLogs table)? (y/n, default: y)
   - If yes: Database log level? (D=Debug, I=Information, W=Warning, E=Error, default: D)
3. Enable email history table (EmailHistory)? (y/n, default: y)

**Shortcuts:**
- Yes/No: Just type `y` or `n`
- Log Levels: Just type `D`, `I`, `W`, or `E` (or full names)
- Defaults: Just press Enter

## Log Level Hierarchy

```
Debug    → Database only (all details)
Information → Console, File, Database (important events)
Warning  → All sinks
Error    → All sinks
```

## What Gets Logged at Each Level

### Debug Level
Logged to: **Database only**

- Command line arguments
- Email parameters (to, subject, attachments, from)
- Database configuration loading
- Table initialization
- Configuration loaded from database
- Sender email override
- Email send operation start
- Recipients count
- HTML body source (file/string)
- Attachment processing
- Email sent successfully (with attempt number)
- Performance metrics

### Information Level
Logged to: **Console, File, Database**

- Application started
- Application ended
- EmailService initialization
- Sending email via Graph API
- EMAIL_SENT event (subject and recipients)
- Email sent successfully (final message)
- Setup completed
- Errors and warnings

## Example Output

### Console (Information - Clean)
```
[17:30:38 INF] === Mailer Application Started ===
[17:30:39 INF] Initializing EmailService for sender: ma.monza@saccosrl.it
[17:30:39 INF] EMAIL_SENT: Test sender to ma.monza@saccosrl.it
[17:30:40 INF] Email sent successfully
✓ Email sent successfully!
[17:30:40 INF] === Mailer Application Ended ===
```

### File (Information - Same as Console)
```
2025-12-10 17:30:38.123 +01:00 [INF] === Mailer Application Started ===
2025-12-10 17:30:39.456 +01:00 [INF] Initializing EmailService for sender: ma.monza@saccosrl.it
2025-12-10 17:30:39.789 +01:00 [INF] EMAIL_SENT: Test sender to ma.monza@saccosrl.it
2025-12-10 17:30:40.012 +01:00 [INF] Email sent successfully
2025-12-10 17:30:40.345 +01:00 [INF] === Mailer Application Ended ===
```

### Database (Debug - All Details)
Query `MailerLogs` table to see all events including:
```sql
SELECT TOP 100 
    TimeStamp, 
    Level, 
    Message,
    Properties
FROM MailerLogs
ORDER BY TimeStamp DESC;
```

Includes debug events like:
- "Command line arguments: --to user@example.com..."
- "Email parameters - To: user@example.com, Subject: Test..."
- "Loading database configuration"
- "Configuration loaded successfully. Sender: sender@example.com"
- And all Information-level events

## Configuration Code

### Initial Setup (Before Database)
```csharp
// Minimal console-only logger
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(
        restrictedToMinimumLevel: LogEventLevel.Information);

Log.Logger = loggerConfig.CreateLogger();
```

### After Database Connection (Conditional Sinks)
```csharp
var logConfig = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(
        restrictedToMinimumLevel: LogEventLevel.Information);

// Add file logging if enabled
if (dbConfig.EnableFileLogging)
{
    var fileLogLevel = ParseLogLevel(dbConfig.FileLogLevel);
    logConfig.WriteTo.File(
        path: "logs/mailer-.log",
        restrictedToMinimumLevel: fileLogLevel,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30);
}

// Add database logging if enabled
if (dbConfig.EnableDatabaseLogging)
{
    var dbLogLevel = ParseLogLevel(dbConfig.DatabaseLogLevel);
    logConfig.WriteTo.MSSqlServer(
        connectionString: connectionString,
        restrictedToMinimumLevel: dbLogLevel,
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "MailerLogs",
            AutoCreateSqlTable = true
        });
}

// Initialize EmailHistory if enabled
if (dbConfig.EnableEmailHistory)
{
    EmailHistoryService.SetConnectionString(connectionString);
    EmailHistoryService.InitializeTable();
}

logConfig.Enrich.WithProperty("Application", "Mailer");
Log.Logger = logConfig.CreateLogger();
```

## Querying Database Logs

### All Events for an Email Send
```sql
SELECT 
    TimeStamp,
    Level,
    Message,
    Exception
FROM MailerLogs
WHERE Message LIKE '%ma.monza@saccosrl.it%'
ORDER BY TimeStamp DESC;
```

### Debug Events Only
```sql
SELECT 
    TimeStamp,
    Message
FROM MailerLogs
WHERE Level = 'Debug'
ORDER BY TimeStamp DESC;
```

### Performance Metrics
```sql
SELECT 
    TimeStamp,
    Message,
    JSON_VALUE(Properties, '$.DurationMs') AS DurationMs,
    JSON_VALUE(Properties, '$.Attempt') AS Attempt,
    JSON_VALUE(Properties, '$.Success') AS Success
FROM MailerLogs
WHERE JSON_VALUE(Properties, '$.EventType') = 'PerformanceMetric'
ORDER BY TimeStamp DESC;
```

## Benefits of This Configuration

1. **Clean Console**: Only important events, easy to read during execution
2. **Flexible File Logs**: Choose level based on needs (Debug for troubleshooting, Information for production)
3. **Flexible Database**: Choose level based on storage/query needs
4. **Email History**: Dedicated table with one row per email for easy querying
5. **Configurable**: Enable/disable each sink independently
6. **Performance**: Disabled sinks don't consume resources
7. **Storage**: Can disable database logging to save space

## Changing Log Levels

To change logging configuration, run `--setup` again and reconfigure Step 3.

Your choices are saved in `db.config.json` and will be used for all future runs.

## Related Documentation

- [EMAIL_LOGGING.md](EMAIL_LOGGING.md) - Serilog configuration and MailerLogs table
- [EMAIL_HISTORY.md](EMAIL_HISTORY.md) - Dedicated EmailHistory table
- [PERFORMANCE.md](PERFORMANCE.md) - Performance monitoring and metrics

---

**Current Configuration Options:**
- Console: Information (always enabled, fixed level)
- File: Configurable enable/disable + level (default: enabled, Information)
- Database (MailerLogs): Configurable enable/disable + level (default: enabled, Debug)
- EmailHistory: Configurable enable/disable (default: enabled)
