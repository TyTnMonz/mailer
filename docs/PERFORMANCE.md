# Performance Monitoring

## Overview

The Mailer application includes comprehensive performance monitoring for all email send operations. Metrics are automatically logged to help you track reliability and performance.

## Metrics Tracked

### Email Send Duration
- **Measurement**: Milliseconds from start to completion
- **Includes**: All retries and delays
- **Logged**: Every email send (success or failure)

### Retry Attempts
- **Tracks**: Number of attempts needed
- **Range**: 1 to maxRetries + 1 (default: 1-4)
- **Logged**: With each performance metric

### Success Rate
- **Boolean**: true/false
- **Logged**: Final result after all retries
- **Queryable**: Easy to calculate success rates

## Automatic Retry Logic

### Configuration
- **Default retries**: 3 attempts (4 total tries)
- **Backoff strategy**: Exponential
- **Delays**: 1s, 2s, 4s, 8s...
- **Formula**: `2^(attempt-1) * 1000ms`

### Behavior
1. **First attempt** fails → Wait 1 second
2. **Second attempt** fails → Wait 2 seconds
3. **Third attempt** fails → Wait 4 seconds
4. **Fourth attempt** fails → Throw exception

### Log Output
```
[17:30:15 INF] Starting email send operation. Subject: Test, MaxRetries: 3
[17:30:16 WRN] ⚠ Attempt 1/4 failed: Network timeout. Retrying in 1000ms...
[17:30:17 WRN] ⚠ Attempt 2/4 failed: Network timeout. Retrying in 2000ms...
[17:30:19 INF] ✓ Email sent successfully on attempt 3/4 in 4523ms
```

## Database Logging

All performance metrics are logged to the `MailerLogs` table with structured properties.

### Performance Metric Properties

| Property | Type | Description |
|----------|------|-------------|
| EventType | string | "PerformanceMetric" |
| Operation | string | "EmailSend" |
| DurationMs | long | Total duration in milliseconds |
| Attempt | int | Final attempt number |
| Success | bool | true if sent, false if failed |

### Example Queries

#### Average Send Duration
```sql
SELECT 
    AVG(CAST(JSON_VALUE(Properties, '$.DurationMs') AS BIGINT)) AS AvgDurationMs,
    COUNT(*) AS TotalEmails
FROM MailerLogs
WHERE Message LIKE '%performance%'
  AND CAST(TimeStamp AS DATE) = CAST(GETDATE() AS DATE);
```

#### Success Rate by Hour
```sql
SELECT 
    DATEPART(HOUR, TimeStamp) AS Hour,
    COUNT(*) AS Total,
    SUM(CASE WHEN JSON_VALUE(Properties, '$.Success') = 'true' THEN 1 ELSE 0 END) AS Successful,
    CAST(SUM(CASE WHEN JSON_VALUE(Properties, '$.Success') = 'true' THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS SuccessRate
FROM MailerLogs
WHERE Message LIKE '%performance%'
  AND CAST(TimeStamp AS DATE) = CAST(GETDATE() AS DATE)
GROUP BY DATEPART(HOUR, TimeStamp)
ORDER BY Hour;
```

#### Emails Requiring Retries
```sql
SELECT 
    TimeStamp,
    Message,
    JSON_VALUE(Properties, '$.Attempt') AS Attempts,
    JSON_VALUE(Properties, '$.DurationMs') AS DurationMs
FROM MailerLogs
WHERE Message LIKE '%performance%'
  AND CAST(JSON_VALUE(Properties, '$.Attempt') AS INT) > 1
ORDER BY TimeStamp DESC;
```

#### Performance Over Time
```sql
SELECT 
    CAST(TimeStamp AS DATE) AS Date,
    AVG(CAST(JSON_VALUE(Properties, '$.DurationMs') AS BIGINT)) AS AvgDurationMs,
    MIN(CAST(JSON_VALUE(Properties, '$.DurationMs') AS BIGINT)) AS MinDurationMs,
    MAX(CAST(JSON_VALUE(Properties, '$.DurationMs') AS BIGINT)) AS MaxDurationMs,
    COUNT(*) AS TotalEmails
FROM MailerLogs
WHERE Message LIKE '%performance%'
  AND TimeStamp >= DATEADD(DAY, -30, GETDATE())
GROUP BY CAST(TimeStamp AS DATE)
ORDER BY Date DESC;
```

## Console Output

Performance information is displayed in the console with visual indicators:

### Success
```
[17:30:15 INF] ✓ Email sent successfully on attempt 1/4 in 1234ms
```

### Retry
```
[17:30:15 WRN] ⚠ Attempt 1/4 failed: Network timeout. Retrying in 1000ms...
```

### Failure
```
[17:30:15 ERR] ✗ Email send failed after 4 attempts in 8765ms
```

## Customizing Retry Behavior

When using the `EmailService` as a library, you can customize retry behavior:

```csharp
var emailService = new EmailService(graphConfig);

// Custom retry count
await emailService.SendMailAsync(
    to: new[] { "user@example.com" },
    subject: "Test",
    htmlBodyOrPath: "<h1>Hello</h1>",
    maxRetries: 5  // 6 total attempts
);

// No retries
await emailService.SendMailAsync(
    to: new[] { "user@example.com" },
    subject: "Test",
    htmlBodyOrPath: "<h1>Hello</h1>",
    maxRetries: 0  // 1 attempt only
);
```

## Performance Best Practices

1. **Monitor trends**: Track average duration over time
2. **Set alerts**: Alert on high retry rates or slow sends
3. **Analyze failures**: Review failed sends for patterns
4. **Optimize content**: Large attachments increase duration
5. **Network quality**: Poor connectivity increases retries

## Troubleshooting

### High Retry Rates
- Check network connectivity
- Verify Microsoft Graph API service status
- Review Azure AD app permissions
- Check SQL Server connectivity

### Slow Send Times
- Large attachments increase duration
- Network latency to Microsoft servers
- SQL Server performance (logging overhead)
- Multiple recipients increase processing time

### All Sends Failing
- Verify Azure AD credentials are valid
- Check client secret expiration
- Confirm Mail.Send permission granted
- Review error messages in logs

---

**See Also:**
- [EMAIL_LOGGING.md](EMAIL_LOGGING.md) - Comprehensive logging documentation
- [SECURITY.md](SECURITY.md) - Security best practices
