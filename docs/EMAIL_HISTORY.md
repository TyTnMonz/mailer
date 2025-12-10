# Email History Table

## Overview

The `EmailHistory` table provides a dedicated, clean database table for tracking all email sends with one row per email. This is separate from the general application logs (`MailerLogs`) and provides an easy-to-query email audit trail.

## Table Structure

```sql
CREATE TABLE [dbo].[EmailHistory] (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(),
    Sender NVARCHAR(255) NOT NULL,
    ToRecipients NVARCHAR(MAX) NOT NULL,
    CcRecipients NVARCHAR(MAX) NULL,
    BccRecipients NVARCHAR(MAX) NULL,
    Subject NVARCHAR(500) NOT NULL,
    BodyPreview NVARCHAR(1000) NULL,
    AttachmentCount INT NOT NULL DEFAULT 0,
    AttachmentNames NVARCHAR(MAX) NULL,
    Status NVARCHAR(50) NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    DurationMs BIGINT NOT NULL,
    AttemptCount INT NOT NULL DEFAULT 1
);

-- Indexes for performance
CREATE INDEX IX_EmailHistory_Timestamp ON [dbo].[EmailHistory](Timestamp DESC);
CREATE INDEX IX_EmailHistory_Sender ON [dbo].[EmailHistory](Sender);
CREATE INDEX IX_EmailHistory_Status ON [dbo].[EmailHistory](Status);
```

## Fields

| Field | Type | Description |
|-------|------|-------------|
| `Id` | INT | Auto-increment primary key |
| `Timestamp` | DATETIME2 | When the email was sent |
| `Sender` | NVARCHAR(255) | Email address of sender |
| `ToRecipients` | NVARCHAR(MAX) | Semicolon-separated To recipients |
| `CcRecipients` | NVARCHAR(MAX) | Semicolon-separated CC recipients (nullable) |
| `BccRecipients` | NVARCHAR(MAX) | Semicolon-separated BCC recipients (nullable) |
| `Subject` | NVARCHAR(500) | Email subject line |
| `BodyPreview` | NVARCHAR(1000) | First 500 characters of email body |
| `AttachmentCount` | INT | Number of attachments |
| `AttachmentNames` | NVARCHAR(MAX) | Semicolon-separated attachment filenames |
| `Status` | NVARCHAR(50) | "Success" or "Failed" |
| `ErrorMessage` | NVARCHAR(MAX) | Error message if failed (nullable) |
| `DurationMs` | BIGINT | Time taken to send in milliseconds |
| `AttemptCount` | INT | Number of attempts (1-4 with default retry) |

## Example Queries

### All Emails Sent Today
```sql
SELECT 
    Timestamp,
    Sender,
    ToRecipients,
    Subject,
    Status,
    DurationMs
FROM EmailHistory
WHERE CAST(Timestamp AS DATE) = CAST(GETDATE() AS DATE)
ORDER BY Timestamp DESC;
```

### Failed Emails
```sql
SELECT 
    Timestamp,
    Sender,
    ToRecipients,
    Subject,
    ErrorMessage,
    AttemptCount
FROM EmailHistory
WHERE Status = 'Failed'
ORDER BY Timestamp DESC;
```

### Emails by Sender
```sql
SELECT 
    Timestamp,
    ToRecipients,
    Subject,
    Status,
    DurationMs
FROM EmailHistory
WHERE Sender = 'ma.monza@saccosrl.it'
ORDER BY Timestamp DESC;
```

### Emails with Attachments
```sql
SELECT 
    Timestamp,
    Sender,
    ToRecipients,
    Subject,
    AttachmentCount,
    AttachmentNames
FROM EmailHistory
WHERE AttachmentCount > 0
ORDER BY Timestamp DESC;
```

### Performance Statistics
```sql
SELECT 
    Status,
    COUNT(*) AS TotalEmails,
    AVG(DurationMs) AS AvgDurationMs,
    MIN(DurationMs) AS MinDurationMs,
    MAX(DurationMs) AS MaxDurationMs,
    AVG(CAST(AttemptCount AS FLOAT)) AS AvgAttempts
FROM EmailHistory
GROUP BY Status;
```

### Emails Requiring Retries
```sql
SELECT 
    Timestamp,
    Sender,
    ToRecipients,
    Subject,
    AttemptCount,
    DurationMs,
    Status
FROM EmailHistory
WHERE AttemptCount > 1
ORDER BY Timestamp DESC;
```

### Daily Email Volume
```sql
SELECT 
    CAST(Timestamp AS DATE) AS Date,
    COUNT(*) AS TotalEmails,
    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) AS Successful,
    SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) AS Failed,
    AVG(DurationMs) AS AvgDurationMs
FROM EmailHistory
WHERE Timestamp >= DATEADD(DAY, -30, GETDATE())
GROUP BY CAST(Timestamp AS DATE)
ORDER BY Date DESC;
```

### Search by Recipient
```sql
SELECT 
    Timestamp,
    Sender,
    ToRecipients,
    CcRecipients,
    Subject,
    Status
FROM EmailHistory
WHERE ToRecipients LIKE '%user@example.com%'
   OR CcRecipients LIKE '%user@example.com%'
   OR BccRecipients LIKE '%user@example.com%'
ORDER BY Timestamp DESC;
```

### Search by Subject
```sql
SELECT 
    Timestamp,
    Sender,
    ToRecipients,
    Subject,
    Status,
    DurationMs
FROM EmailHistory
WHERE Subject LIKE '%Monthly Report%'
ORDER BY Timestamp DESC;
```

## Differences from MailerLogs

| Feature | EmailHistory | MailerLogs |
|---------|-------------|------------|
| **Purpose** | Email audit trail | Application logging |
| **Rows per email** | 1 row | Multiple rows (events) |
| **Data structure** | Dedicated columns | JSON properties |
| **Query complexity** | Simple SQL | JSON parsing |
| **Log level** | All emails | Information+ |
| **Content** | Email-specific | All app events |

## Automatic Table Creation

The `EmailHistory` table is automatically created when the application first runs if it doesn't exist. The table creation includes:
- Primary key with auto-increment
- Three indexes for common queries (Timestamp, Sender, Status)
- Proper data types and constraints

## Usage

The table is populated automatically by the `EmailHistoryService` whenever an email is sent (success or failure). No manual intervention is required.

## Data Retention

Consider implementing a data retention policy based on your requirements:

```sql
-- Delete emails older than 90 days
DELETE FROM EmailHistory
WHERE Timestamp < DATEADD(DAY, -90, GETDATE());
```

Or create a scheduled job to archive old records to a separate table.

## Privacy Considerations

The `EmailHistory` table contains:
- Email addresses (sender and recipients)
- Subject lines
- Body preview (first 500 characters)
- Attachment filenames

Ensure compliance with your organization's data retention and privacy policies (GDPR, etc.).

---

**See Also:**
- [EMAIL_LOGGING.md](EMAIL_LOGGING.md) - General application logging with Serilog
- [PERFORMANCE.md](PERFORMANCE.md) - Performance monitoring and retry logic
