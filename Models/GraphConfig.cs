namespace Mailer.Models;

/// <summary>
/// Microsoft Graph API configuration
/// </summary>
public class GraphConfig
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Recipients { get; set; } = string.Empty;

    /// <summary>
    /// Validates that all required fields are populated
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(TenantId) &&
               !string.IsNullOrWhiteSpace(ClientId) &&
               !string.IsNullOrWhiteSpace(ClientSecret) &&
               !string.IsNullOrWhiteSpace(SenderEmail);
    }
}
