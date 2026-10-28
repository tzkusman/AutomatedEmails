using System;

namespace MailCraftAI.Models;

public class HistoryRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string SourceType { get; set; } = "Outlook";
    public string OriginalSubject { get; set; } = string.Empty;
    public string OriginalSender { get; set; } = string.Empty;
    public string OriginalSummary { get; set; } = string.Empty;
    public string UserInstruction { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string ReplyLength { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string GeneratedSubject { get; set; } = string.Empty;
    public string GeneratedReply { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
}
