namespace MailCraftAI.Services.Context;

public record ContextTrimResult(
    bool IsTrimmed,
    int OriginalCharCount,
    int EstimatedTokens,
    string ProcessedText,
    string? WarningMessage
);

public interface IContextTrimmerService
{
    ContextTrimResult ProcessContext(string contextText, int maxCharThreshold = 12000, bool autoTrim = true);
}
