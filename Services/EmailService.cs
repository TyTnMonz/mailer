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
    /// Sends an email using Microsoft Graph API.
    /// </summary>
    /// <param name="to">List of recipient email addresses (required).</param>
    /// <param name="cc">List of CC recipient email addresses (optional).</param>
    /// <param name="bcc">List of BCC recipient email addresses (optional).</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="htmlBodyOrPath">HTML body content or path to HTML file.</param>
    /// <param name="isBodyAFile">If true, htmlBodyOrPath is treated as a file path; otherwise, as HTML content.</param>
    /// <param name="attachmentPaths">List of file paths to attach (optional).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendMailAsync(
        IEnumerable<string> to,
        IEnumerable<string>? cc = null,
        IEnumerable<string>? bcc = null,
        string subject = "",
        string htmlBodyOrPath = "",
        bool isBodyAFile = false,
        IEnumerable<string>? attachmentPaths = null)
    {
        _logger.Information("Starting email send operation. Subject: {Subject}, IsBodyAFile: {IsBodyAFile}", 
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

            _logger.Information("Recipients - To: {ToCount}, CC: {CcCount}, BCC: {BccCount}", 
                toList.Count, 
                cc?.Count() ?? 0, 
                bcc?.Count() ?? 0);

            _logger.Debug("To recipients: {ToRecipients}", string.Join(", ", toList));

            // Load HTML body
            string htmlBody;
            if (isBodyAFile)
            {
                _logger.Information("Loading HTML body from file: {FilePath}", htmlBodyOrPath);
                
                if (!File.Exists(htmlBodyOrPath))
                {
                    _logger.Error("HTML body file not found: {FilePath}", htmlBodyOrPath);
                    throw new FileNotFoundException($"HTML body file not found: {htmlBodyOrPath}");
                }

                htmlBody = await File.ReadAllTextAsync(htmlBodyOrPath);
                _logger.Information("HTML body loaded successfully from file. Length: {Length} characters", htmlBody.Length);
            }
            else
            {
                htmlBody = htmlBodyOrPath;
                _logger.Information("Using HTML body from string. Length: {Length} characters", htmlBody.Length);
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
                _logger.Information("Processing {AttachmentCount} attachment(s)", attachmentList.Count);
                
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
                    _logger.Information("Attachment added: {FileName}, Size: {Size} bytes", fileName, fileBytes.Length);
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

            _logger.Information("Email sent successfully. Subject: {Subject}, To: {ToRecipients}", 
                subject, string.Join(", ", toList));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send email. Subject: {Subject}, Error: {ErrorMessage}", 
                subject, ex.Message);
            throw;
        }
    }
}
