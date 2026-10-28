using System.Collections.Generic;

namespace MailCraftAI.Services.Security;

public record PiiMaskResult(
    string MaskedText,
    IReadOnlyDictionary<string, string> ReplacementMap,
    int ItemsMaskedCount
);

public interface IPiiMaskingService
{
    PiiMaskResult Mask(string input);
    string Unmask(string maskedText, IReadOnlyDictionary<string, string> replacementMap);
}
