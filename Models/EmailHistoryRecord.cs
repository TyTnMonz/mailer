namespace Mailer.Models;

/// <summary>
/// Represents an email history record in the database
/// </summary>
public class EmailHistoryRecord
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string ToRecipients { get; set; } = string.Empty;
    public string? CcRecipients { get; set; }
    public string? BccRecipients { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? BodyPreview { get; set; }
    public int AttachmentCount { get; set; }
    public string? AttachmentNames { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public int AttemptCount { get; set; }
}
