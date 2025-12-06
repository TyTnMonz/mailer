# Security Documentation

## Overview

The Mailer application implements a multi-layered security approach to protect sensitive credentials using SQL Server database storage combined with optional file system security features.

## Configuration Storage

### Database Storage (Primary)

Microsoft Graph API credentials are stored securely in SQL Server:

- **Location**: SQL Server database table (configurable, default: `SaccoGest`)
- **Fields**: TenantID, ClientID, ClientSec, Sender, To (Recipients)
- **Access Control**: Protected by SQL Server authentication and permissions
- **Encryption**: SQL Server Transparent Data Encryption (TDE) can be enabled
- **Audit**: All access can be logged via SQL Server audit features

### Database Configuration File

The `db.config.json` file contains database connection details:

- **Location**: Same directory as `Mailer.exe`
- **Contents**: Server IP, database name, table name, authentication method
- **Security Options**: 
  - **Secured**: Hidden, System, ReadOnly attributes (optional, user choice during setup)
  - **Normal**: Regular file (easier to copy/backup)

## Security Guarantees

### ✅ What This Protects Against

1. **Credential Exposure**: Graph API credentials never stored in plain text files
2. **Centralized Management**: All credentials in SQL Server, easy to rotate
3. **Access Control**: SQL Server permissions control who can read credentials
4. **Audit Trail**: SQL Server can log all credential access
5. **Source Control Leaks**: `.gitignore` prevents committing `db.config.json`
6. **Network Security**: Credentials transmitted over encrypted SQL connection

### ⚠️ Limitations

1. **SQL Server Access**: Anyone with SQL access can read credentials
2. **Database Administrator**: DBAs have full access to credentials
3. **Connection String**: `db.config.json` contains database connection details
4. **Memory**: Credentials exist in memory while application is running
5. **Logging**: Email bodies and recipients are logged to database (privacy consideration)

## Best Practices

### For Development

- Use separate SQL Server database for development
- Use different Azure AD app registration for dev/staging/production
- Never commit `db.config.json` to source control
- Use Windows Authentication when possible (no password in connection string)

### For Production

1. **SQL Server Security**:
   - Enable Transparent Data Encryption (TDE)
   - Use Windows Authentication instead of SQL authentication
   - Restrict database permissions to minimum required
   - Enable SQL Server audit logging

2. **Azure AD App Registration**:
   - Use separate app registration for production
   - Grant only `Mail.Send` permission (application permission)
   - Regularly rotate client secrets (set expiration)
   - Monitor app usage in Azure AD logs

3. **Network Security**:
   - Use encrypted SQL Server connections (SSL/TLS)
   - Restrict network access to SQL Server
   - Consider using Azure SQL with firewall rules

4. **Application Security**:
   - Run application under service account with minimum privileges
   - Secure the application directory
   - Monitor log files for suspicious activity

### For Deployment

- Run setup on target machine under the service account
- Choose appropriate security level for `db.config.json`
- Document which SQL account/Windows user has access
- Test credential rotation procedure

## Email Logging Privacy

The application logs all email sends to the `MailerLogs` table:

**Logged Information**:
- Recipients (To, CC, BCC)
- Subject
- Body (full HTML content)
- Timestamp
- Result (Success/Failed)
- Error messages (if failed)

**Privacy Considerations**:
- Email bodies are stored in plain text
- Recipient information is logged
- Consider data retention policies
- Ensure compliance with privacy regulations (GDPR, etc.)
- Restrict access to `MailerLogs` table

**Recommendations**:
- Implement log retention policy (e.g., delete logs older than 90 days)
- Restrict SQL permissions on `MailerLogs` table
- Consider encrypting sensitive columns
- Document in privacy policy

## Troubleshooting

### "Database connection not configured"

**Cause**: `db.config.json` file not found or invalid

**Solution**: Run setup utility:
```cmd
Mailer.exe --setup
```

### "Configuration not found in database"

**Cause**: Graph API credentials not saved to database

**Solution**: Run setup again to save credentials

### "Access denied" to db.config.json

**Cause**: File has restrictive attributes (Hidden, System, ReadOnly)

**Solution**: The application handles this automatically, but if needed:
```cmd
attrib -h -s -r db.config.json
```

## Comparison with Alternatives

| Method | Security | Ease of Use | Platform | Best For |
|--------|----------|-------------|----------|----------|
| **SQL Server (Current)** | Good | Easy | Windows/Linux | Centralized credential management |
| **Environment Variables** | Good | Easy | Cross-platform | Containers, cloud deployments |
| **Azure Key Vault** | Excellent | Moderate | Cross-platform | Production environments |
| **Managed Identity** | Excellent | Easy | Azure only | Azure-hosted applications |
| **Hardcoded** | ❌ Poor | Easy | Cross-platform | ❌ Never use |

## Security Checklist

Before deploying to production:

- [ ] Use separate Azure AD app registration for production
- [ ] Verify client secret has appropriate expiration date
- [ ] Confirm app registration has only `Mail.Send` permission
- [ ] Enable SQL Server TDE (Transparent Data Encryption)
- [ ] Use Windows Authentication for SQL Server
- [ ] Restrict SQL Server network access
- [ ] Implement log retention policy for `MailerLogs`
- [ ] Test secret rotation procedure
- [ ] Enable SQL Server audit logging
- [ ] Document which accounts have access
- [ ] Review privacy compliance (GDPR, etc.)

## Additional Resources

- [Microsoft Graph API Security](https://docs.microsoft.com/en-us/graph/security-concept-overview)
- [SQL Server Security](https://docs.microsoft.com/en-us/sql/relational-databases/security/)
- [Azure Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Azure Managed Identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [SQL Server TDE](https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/transparent-data-encryption)
