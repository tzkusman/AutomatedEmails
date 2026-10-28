using System;
using System.Text;
using System.Text.RegularExpressions;

namespace MailCraftAI.Services.Context;

public class ContextTrimmerService : IContextTrimmerService
{
    private static readonly Regex ThreadDividerRegex = new(
        @"(?:-{3,}\s*Original Message\s*-{3,}|_{10,}|From:\s*.+\r?\nSent:\s*.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ContextTrimResult ProcessContext(string contextText, int maxCharThreshold = 12000, bool autoTrim = true)
    {
        if (string.IsNullOrWhiteSpace(contextText))
        {
            return new ContextTrimResult(false, 0, 0, string.Empty, null);
        }

        int originalLen = contextText.Length;
        int estimatedTokens = (int)Math.Ceiling(originalLen / 3.8);

        if (originalLen <= maxCharThreshold || !autoTrim)
        {
            string? warning = originalLen > maxCharThreshold
                ? $"The email context is large ({originalLen:N0} characters / ~{estimatedTokens:N0} tokens). Consider trimming long thread history."
                : null;

            return new ContextTrimResult(false, originalLen, estimatedTokens, contextText, warning);
        }

        // Context exceeds threshold, perform intelligent trimming
        string trimmed = IntelligentTrim(contextText, maxCharThreshold);
        int trimmedTokens = (int)Math.Ceiling(trimmed.Length / 3.8);

        string message = $"Email thread was trimmed from {originalLen:N0} to {trimmed.Length:N0} chars (~{trimmedTokens:N0} tokens) to preserve the most recent and critical context.";

        return new ContextTrimResult(
            IsTrimmed: true,
            OriginalCharCount: originalLen,
            EstimatedTokens: trimmedTokens,
            ProcessedText: trimmed,
            WarningMessage: message
        );
    }

    private static string IntelligentTrim(string text, int maxTarget)
    {
        // Try to split by thread divider
        var matches = ThreadDividerRegex.Matches(text);
        if (matches.Count > 0)
        {
            // First chunk is the latest message
            int firstDividerIndex = matches[0].Index;
            string latestMessage = text[..firstDividerIndex].Trim();

            // If latest message itself is within budget, we keep it plus as much thread context as fits
            if (latestMessage.Length < maxTarget)
            {
                int remainingBudget = maxTarget - latestMessage.Length - 100;
                if (remainingBudget > 200 && firstDividerIndex < text.Length)
                {
                    string threadPortion = text[firstDividerIndex..];
                    if (threadPortion.Length > remainingBudget)
                    {
                        threadPortion = threadPortion[..remainingBudget] + "\n\n[...older email thread history truncated for brevity...]";
                    }
                    return latestMessage + "\n\n" + threadPortion;
                }

                return latestMessage + "\n\n[...older email thread history truncated for brevity...]";
            }
        }

        // Fallback: take head and tail or head portion
        int headLength = (int)(maxTarget * 0.75);
        int tailLength = maxTarget - headLength - 100;

        string head = text[..headLength];
        string tail = text[^tailLength..];

        return $"{head}\n\n[...middle section truncated for brevity...]\n\n{tail}";
    }
}
