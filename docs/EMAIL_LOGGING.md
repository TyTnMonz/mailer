# Email Logging with Serilog

## Overview

The Mailer application uses **Serilog** with multiple sinks to log all application events and email sends to:
- **Console** - Real-time output
- **File** - Daily rolling logs in `logs/mailer-YYYYMMDD.log`
- **Database** - SQL Server table `MailerLogs`

## Database Logging

All logs are automatically written to the `MailerLogs` table in your configured SQL Server database.

### Table Structure

Serilog automatically creates the table with this structure:

```sql
CREATE TABLE [MailerLogs] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Message] NVARCHAR(MAX),
    [MessageTemplate] NVARCHAR(MAX),
    [Level] NVARCHAR(128),
    [TimeStamp] DATETIME NOT NULL,
    [Exception] NVARCHAR(MAX),
    [Properties] NVARCHAR(MAX)
);
```

### Email-Specific Properties

Email sends are logged with enriched properties stored in the `Properties` XML column:

- **EventType**: "EmailSent" or "EmailFailed"
- **Recipients**: All recipients (To, CC, BCC)
- **Subject**: Email subject line
- **Body**: HTML body content
- **Result**: "Success" or "Failed"
- **ErrorMessage**: Error details (if failed)
- **Application**: "Mailer"

## Example Queries

### View All Email Sends
```sql
SELECT 
    Id,
    TimeStamp,
    Level,
    Message,
    Properties
FROM MailerLogs
WHERE Message LIKE '%EMAIL_%'
ORDER BY TimeStamp DESC;
```

### View Successful Emails
```sql
SELECT 
    Id,
    TimeStamp,
    Message,
    Properties
FROM MailerLogs
WHERE Message LIKE '%EMAIL_SENT%'
ORDER BY TimeStamp DESC;
```

### View Failed Emails
```sql
SELECT 
    Id,
    TimeStamp,
    Message,
    Exception,
    Properties
FROM MailerLogs
WHERE Message LIKE '%EMAIL_FAILED%'
ORDER BY TimeStamp DESC;
```

### Extract Email Properties (SQL Server 2016+)
```sql
SELECT 
    Id,
    TimeStamp,
    Level,
    Message,
    JSON_VALUE(Properties, '$.Recipients') AS Recipients,
    JSON_VALUE(Properties, '$.Subject') AS Subject,
    JSON_VALUE(Properties, '$.Result') AS Result
FROM MailerLogs
WHERE Message LIKE '%EMAIL_%'
ORDER BY TimeStamp DESC;
```

### Count Emails by Result
```sql
SELECT 
    JSON_VALUE(Properties, '$.Result') AS Result,
    COUNT(*) AS Count
FROM MailerLogs
WHERE Message LIKE '%EMAIL_%'
GROUP BY JSON_VALUE(Properties, '$.Result');
```

### Emails Sent Today
```sql
SELECT 
    Id,
    TimeStamp,
    Message,
    JSON_VALUE(Properties, '$.Recipients') AS Recipients,
    JSON_VALUE(Properties, '$.Subject') AS Subject
FROM MailerLogs
WHERE CAST(TimeStamp AS DATE) = CAST(GETDATE() AS DATE)
  AND Message LIKE '%EMAIL_%'
ORDER BY TimeStamp DESC;
```

### Search by Recipient
```sql
SELECT 
    Id,
    TimeStamp,
    Message,
    Properties
FROM MailerLogs
WHERE Properties LIKE '%user@example.com%'
  AND Message LIKE '%EMAIL_%'
ORDER BY TimeStamp DESC;
```

## Log Levels

Serilog uses these log levels:
- **Debug**: Detailed diagnostic information
- **Information**: General informational messages (including successful emails)
- **Warning**: Warning messages
- **Error**: Error messages (including failed emails)
- **Fatal**: Critical errors

## Configuration

The Serilog configuration is in `Program.cs`:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/mailer-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.MSSqlServer(connectionString, 
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "MailerLogs",
            AutoCreateSqlTable = true
        })
    .Enrich.WithProperty("Application", "Mailer")
    .CreateLogger();
```

## File Logs

File logs are stored in:
```
logs/mailer-YYYYMMDD.log
```

- **Rolling**: New file created daily
- **Retention**: Last 30 days kept
- **Format**: Timestamped with level and message

## Privacy Considerations

**Important**: Email bodies and recipient information are logged to the database.

Consider:
- **Data retention**: Regularly archive or purge old logs
- **Sensitive content**: Email bodies are stored in plain text (in XML Properties)
- **Database security**: Ensure proper SQL Server permissions
- **Compliance**: Review with your data protection policies

## Disabling Database Logging

To disable database logging, remove the `.WriteTo.MSSqlServer()` line from `Program.cs`.

File and console logging will continue to work.

## Performance

- **Batching**: Serilog batches writes for performance (configurable)
- **Async**: Database writes are asynchronous
- **Minimal impact**: Email sending is not blocked by logging

---

**Version**: 2.0 (Serilog-based)
**Updated**: 2025-12-06
