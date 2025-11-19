namespace Mailer.Models;

/// <summary>
/// Configuration model for Microsoft Graph API settings.
/// </summary>
public class GraphConfig
{
    /// <summary>
    /// Azure AD tenant ID (Directory ID).
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Application (client) ID from Azure AD app registration.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret value from Azure AD app registration.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Email address to send from (must exist in Office 365 tenant).
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;
}
