using System;
using System.Collections.Generic;

namespace MailCraftAI.Models;

public class EmailContext
{
    public InputSourceType SourceType { get; set; } = InputSourceType.PastedText;
    public string Subject { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public List<string> RecipientsTo { get; set; } = [];
    public List<string> RecipientsCc { get; set; } = [];
    public DateTime? ReceivedTime { get; set; }
    public string BodyText { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public List<string> AttachmentNames { get; set; } = [];
    public string ConversationHistory { get; set; } = string.Empty;

    // Outlook specific reference identifiers
    public bool IsFromOutlook => !string.IsNullOrEmpty(OutlookEntryId);
    public string? OutlookEntryId { get; set; }
    public string? OutlookStoreId { get; set; }
    public string? SourceFilePath { get; set; }

    public string FormattedSender => string.IsNullOrWhiteSpace(SenderName) 
        ? SenderEmail 
        : $"{SenderName} <{SenderEmail}>";

    public string SummaryText
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Subject)) parts.Add($"Subject: {Subject}");
            if (!string.IsNullOrWhiteSpace(SenderName) || !string.IsNullOrWhiteSpace(SenderEmail)) parts.Add($"From: {FormattedSender}");
            if (ReceivedTime.HasValue) parts.Add($"Date: {ReceivedTime.Value:g}");
            if (AttachmentNames.Count > 0) parts.Add($"Attachments: {string.Join(", ", AttachmentNames)}");
            return string.Join(" | ", parts);
        }
    }
}
