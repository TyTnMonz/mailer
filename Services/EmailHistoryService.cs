using Mailer.Models;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Mailer.Services;

/// <summary>
/// Service for logging email history to a dedicated database table
/// </summary>
public class EmailHistoryService
{
    private static string? _connectionString;
    private static readonly ILogger _logger = Log.Logger;
    private const string TableName = "EmailHistory";

    /// <summary>
    /// Sets the database connection string
    /// </summary>
    public static void SetConnectionString(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Initializes the EmailHistory table if it doesn't exist
    /// </summary>
    public static void InitializeTable()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Connection string not set");

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string createTableSql = $@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{TableName}' AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    CREATE TABLE [dbo].[{TableName}] (
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
                    
                    CREATE INDEX IX_EmailHistory_Timestamp ON [dbo].[{TableName}](Timestamp DESC);
                    CREATE INDEX IX_EmailHistory_Sender ON [dbo].[{TableName}](Sender);
                    CREATE INDEX IX_EmailHistory_Status ON [dbo].[{TableName}](Status);
                END";

            using var command = new SqlCommand(createTableSql, connection);
            command.ExecuteNonQuery();

            _logger.Debug("EmailHistory table initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize EmailHistory table");
            throw;
        }
    }

    /// <summary>
    /// Logs an email send event to the database
    /// </summary>
    public static void LogEmail(EmailHistoryRecord record)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.Warning("Cannot log email history: connection string not set");
            return;
        }

        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string insertSql = $@"
                INSERT INTO [dbo].[{TableName}] 
                (Timestamp, Sender, ToRecipients, CcRecipients, BccRecipients, Subject, 
                 BodyPreview, AttachmentCount, AttachmentNames, Status, ErrorMessage, DurationMs, AttemptCount)
                VALUES 
                (@Timestamp, @Sender, @ToRecipients, @CcRecipients, @BccRecipients, @Subject,
                 @BodyPreview, @AttachmentCount, @AttachmentNames, @Status, @ErrorMessage, @DurationMs, @AttemptCount)";

            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("@Timestamp", record.Timestamp);
            command.Parameters.AddWithValue("@Sender", record.Sender);
            command.Parameters.AddWithValue("@ToRecipients", record.ToRecipients);
            command.Parameters.AddWithValue("@CcRecipients", (object?)record.CcRecipients ?? DBNull.Value);
            command.Parameters.AddWithValue("@BccRecipients", (object?)record.BccRecipients ?? DBNull.Value);
            command.Parameters.AddWithValue("@Subject", record.Subject);
            command.Parameters.AddWithValue("@BodyPreview", (object?)record.BodyPreview ?? DBNull.Value);
            command.Parameters.AddWithValue("@AttachmentCount", record.AttachmentCount);
            command.Parameters.AddWithValue("@AttachmentNames", (object?)record.AttachmentNames ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", record.Status);
            command.Parameters.AddWithValue("@ErrorMessage", (object?)record.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@DurationMs", record.DurationMs);
            command.Parameters.AddWithValue("@AttemptCount", record.AttemptCount);

            command.ExecuteNonQuery();

            _logger.Debug("Email history logged successfully: {Subject} to {Recipients}", 
                record.Subject, record.ToRecipients);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to log email history for subject: {Subject}", record.Subject);
            // Don't throw - logging failure shouldn't stop email sending
        }
    }
}
