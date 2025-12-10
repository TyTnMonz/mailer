using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Mailer.Models;
using Serilog;

namespace Mailer.Services;

/// <summary>
/// Service for sending emails using Microsoft Graph API.
/// </summary>
public class EmailService
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _senderEmail;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the EmailService.
    /// </summary>
    /// <param name="config">Microsoft Graph API configuration.</param>
    /// <param name="logger">Optional logger instance. If not provided, uses the global Log.Logger.</param>
    public EmailService(GraphConfig config, ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;

        if (config == null)
        {
            _logger.Error("EmailService initialization failed: config is null");
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.TenantId))
        {
            _logger.Error("EmailService initialization failed: TenantId is required");
            throw new ArgumentException("TenantId is required.", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.ClientId))
        {
            _logger.Error("EmailService initialization failed: ClientId is required");
            throw new ArgumentException("ClientId is required.", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.ClientSecret))
        {
            _logger.Error("EmailService initialization failed: ClientSecret is required");
            throw new ArgumentException("ClientSecret is required.", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.SenderEmail))
        {
            _logger.Error("EmailService initialization failed: SenderEmail is required");
            throw new ArgumentException("SenderEmail is required.", nameof(config));
        }

        _senderEmail = config.SenderEmail;

        _logger.Information("Initializing EmailService for sender: {SenderEmail}", _senderEmail);

        // Create client credentials for OAuth2 authentication
        var clientSecretCredential = new ClientSecretCredential(
            config.TenantId,
            config.ClientId,
            config.ClientSecret
        );

        // Initialize Graph client with authentication
        _graphClient = new GraphServiceClient(clientSecretCredential);

        _logger.Information("EmailService initialized successfully with TenantId: {TenantId}, ClientId: {ClientId}", 
            config.TenantId, config.ClientId);
    }

    /// <summary>
    /// Sends an email using Microsoft Graph API with automatic retry and performance monitoring.
    /// </summary>
    /// <param name="to">List of recipient email addresses (required).</param>
    /// <param name="cc">List of CC recipient email addresses (optional).</param>
    /// <param name="bcc">List of BCC recipient email addresses (optional).</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="htmlBodyOrPath">HTML body content or path to HTML file.</param>
    /// <param name="isBodyAFile">If true, htmlBodyOrPath is treated as a file path; otherwise, as HTML content.</param>
    /// <param name="attachmentPaths">List of file paths to attach (optional).</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendMailAsync(
        IEnumerable<string> to,
        IEnumerable<string>? cc = null,
        IEnumerable<string>? bcc = null,
        string subject = "",
        string htmlBodyOrPath = "",
        bool isBodyAFile = false,
        IEnumerable<string>? attachmentPaths = null,
        int maxRetries = 3)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int attempt = 0;
        Exception? lastException = null;

        _logger.Debug("Starting email send operation. Subject: {Subject}, IsBodyAFile: {IsBodyAFile}, MaxRetries: {MaxRetries}", 
            subject, isBodyAFile, maxRetries);

        while (attempt <= maxRetries)
        {
            attempt++;
            
            try
            {
                await SendMailInternalAsync(to, cc, bcc, subject, htmlBodyOrPath, isBodyAFile, attachmentPaths);
                
                stopwatch.Stop();
                _logger.Information("✓ Email sent successfully on attempt {Attempt}/{MaxAttempts} in {ElapsedMs}ms", 
                    attempt, maxRetries + 1, stopwatch.ElapsedMilliseconds);
                
                // Log performance metrics
                _logger.ForContext("EventType", "PerformanceMetric")
                       .ForContext("Operation", "EmailSend")
                       .ForContext("DurationMs", stopwatch.ElapsedMilliseconds)
                       .ForContext("Attempt", attempt)
                       .ForContext("Success", true)
                       .Information("Email send performance: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return; // Success!
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (attempt > maxRetries)
                {
                    stopwatch.Stop();
                    _logger.Error(ex, "✗ Email send failed after {Attempts} attempts in {ElapsedMs}ms", 
                        attempt, stopwatch.ElapsedMilliseconds);
                    
                    // Log failure metrics
                    _logger.ForContext("EventType", "PerformanceMetric")
                           .ForContext("Operation", "EmailSend")
                           .ForContext("DurationMs", stopwatch.ElapsedMilliseconds)
                           .ForContext("Attempt", attempt)
                           .ForContext("Success", false)
                           .Error(ex, "Email send failed after all retries");
                    
                    throw;
                }
                
                // Calculate exponential backoff delay
                int delayMs = (int)Math.Pow(2, attempt - 1) * 1000; // 1s, 2s, 4s, 8s...
                _logger.Warning("⚠ Attempt {Attempt}/{MaxAttempts} failed: {ErrorMessage}. Retrying in {DelayMs}ms...", 
                    attempt, maxRetries + 1, ex.Message, delayMs);
                
                await Task.Delay(delayMs);
            }
        }
        
        // This should never be reached, but just in case
        throw lastException ?? new Exception("Email send failed for unknown reason");
    }

    /// <summary>
    /// Internal method that performs the actual email send operation.
    /// </summary>
    private async Task SendMailInternalAsync(
        IEnumerable<string> to,
        IEnumerable<string>? cc,
        IEnumerable<string>? bcc,
        string subject,
        string htmlBodyOrPath,
        bool isBodyAFile,
        IEnumerable<string>? attachmentPaths)
    {
        _logger.Debug("Starting email send operation. Subject: {Subject}, IsBodyAFile: {IsBodyAFile}", 
            subject, isBodyAFile);

        try
        {
            // Validate recipients
            var toList = to?.ToList() ?? new List<string>();
            if (toList.Count == 0)
            {
                _logger.Error("Email send failed: No 'To' recipients provided");
                throw new ArgumentException("At least one 'To' recipient is required.", nameof(to));
            }

            _logger.Debug("Recipients - To: {ToCount}, CC: {CcCount}, BCC: {BccCount}", 
                toList.Count, 
                cc?.Count() ?? 0, 
                bcc?.Count() ?? 0);

            _logger.Debug("To recipients: {ToRecipients}", string.Join(", ", toList));

            // Load HTML body
            string htmlBody;
            if (isBodyAFile)
            {
                _logger.Debug("Loading HTML body from file: {FilePath}", htmlBodyOrPath);
                
                if (!File.Exists(htmlBodyOrPath))
                {
                    _logger.Error("HTML body file not found: {FilePath}", htmlBodyOrPath);
                    throw new FileNotFoundException($"HTML body file not found: {htmlBodyOrPath}");
                }

                htmlBody = await File.ReadAllTextAsync(htmlBodyOrPath);
                _logger.Debug("HTML body loaded successfully from file. Length: {Length} characters", htmlBody.Length);
            }
            else
            {
                htmlBody = htmlBodyOrPath;
                _logger.Debug("Using HTML body from string. Length: {Length} characters", htmlBody.Length);
            }

            // Build the message
            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlBody
                },
                ToRecipients = toList.Select(email => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = email }
                }).ToList()
            };

            // Add CC recipients if provided
            if (cc != null && cc.Any())
            {
                message.CcRecipients = cc.Select(email => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = email }
                }).ToList();
                _logger.Debug("CC recipients: {CcRecipients}", string.Join(", ", cc));
            }

            // Add BCC recipients if provided
            if (bcc != null && bcc.Any())
            {
                message.BccRecipients = bcc.Select(email => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = email }
                }).ToList();
                _logger.Debug("BCC recipients: {BccRecipients}", string.Join(", ", bcc));
            }

            // Add attachments if provided
            if (attachmentPaths != null && attachmentPaths.Any())
            {
                var attachmentList = attachmentPaths.ToList();
                _logger.Debug("Processing {AttachmentCount} attachment(s)", attachmentList.Count);
                
                message.Attachments = new List<Attachment>();

                foreach (var filePath in attachmentList)
                {
                    _logger.Debug("Processing attachment: {FilePath}", filePath);
                    
                    if (!File.Exists(filePath))
                    {
                        _logger.Error("Attachment file not found: {FilePath}", filePath);
                        throw new FileNotFoundException($"Attachment file not found: {filePath}");
                    }

                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    var fileName = Path.GetFileName(filePath);

                    var attachment = new FileAttachment
                    {
                        OdataType = "#microsoft.graph.fileAttachment",
                        Name = fileName,
                        ContentBytes = fileBytes
                    };

                    message.Attachments.Add(attachment);
                    _logger.Debug("Attachment added: {FileName}, Size: {Size} bytes", fileName, fileBytes.Length);
                }
            }

            // Send the email
            _logger.Information("Sending email via Microsoft Graph API. From: {SenderEmail}", _senderEmail);
            
            var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true
            };

            await _graphClient.Users[_senderEmail].SendMail.PostAsync(requestBody);

            // Log successful email send with structured data
            string allRecipients = string.Join(", ", toList);
            if (cc != null && cc.Any())
                allRecipients += " (CC: " + string.Join(", ", cc) + ")";
            if (bcc != null && bcc.Any())
                allRecipients += " (BCC: " + string.Join(", ", bcc) + ")";

            _logger.Information("EMAIL_SENT: {Subject} to {Recipients}", 
                subject, allRecipients);
            
            // Log with enriched properties for database
            _logger.ForContext("EventType", "EmailSent")
                   .ForContext("Recipients", allRecipients)
                   .ForContext("Subject", subject)
                   .ForContext("Body", htmlBody)
                   .ForContext("Result", "Success")
                   .Information("Email sent successfully");
        }
        catch (Exception ex)
        {
            // Log failed email send with structured data
            var toList = to?.ToList() ?? new List<string>();
            string allRecipients = string.Join(", ", toList);
            if (cc != null && cc.Any())
                allRecipients += " (CC: " + string.Join(", ", cc) + ")";
            if (bcc != null && bcc.Any())
                allRecipients += " (BCC: " + string.Join(", ", bcc) + ")";

            string bodyContent = isBodyAFile ? $"[File: {htmlBodyOrPath}]" : htmlBodyOrPath;

            _logger.Error(ex, "EMAIL_FAILED: {Subject} to {Recipients}", 
                subject, allRecipients);
            
            // Log with enriched properties for database
            _logger.ForContext("EventType", "EmailFailed")
                   .ForContext("Recipients", allRecipients)
                   .ForContext("Subject", subject)
                   .ForContext("Body", bodyContent)
                   .ForContext("Result", "Failed")
                   .ForContext("ErrorMessage", ex.Message)
                   .Error(ex, "Failed to send email");

            throw;
        }
    }
}
