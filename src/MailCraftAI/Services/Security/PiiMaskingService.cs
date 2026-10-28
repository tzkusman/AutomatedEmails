using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MailCraftAI.Services.Security;

public class PiiMaskingService : IPiiMaskingService
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex IbanRegex = new(
        @"\b[A-Z]{2}[0-9]{2}[A-Z0-9]{4}[0-9]{7}(?:[A-Z0-9]?){0,16}\b",
        RegexOptions.Compiled);

    private static readonly Regex CreditCardRegex = new(
        @"\b(?:\d{4}[ -]?){3}\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex PhoneRegex = new(
        @"(?:\+?\d{1,3}[-.\s]?)?(?:\(?\d{2,4}\)?[-.\s]?)?\d{3,4}[-.\s]?\d{3,4}\b",
        RegexOptions.Compiled);

    public PiiMaskResult Mask(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new PiiMaskResult(string.Empty, new Dictionary<string, string>(), 0);
        }

        var map = new Dictionary<string, string>(); // token -> original
        int emailIdx = 1;
        int phoneIdx = 1;
        int ibanIdx = 1;
        int cardIdx = 1;

        // 1. Mask Emails
        string result = EmailRegex.Replace(input, m =>
        {
            string token = $"{{{{EMAIL_{emailIdx++}}}}}";
            map[token] = m.Value;
            return token;
        });

        // 2. Mask IBANs
        result = IbanRegex.Replace(result, m =>
        {
            string token = $"{{{{IBAN_{ibanIdx++}}}}}";
            map[token] = m.Value;
            return token;
        });

        // 3. Mask Credit Cards
        result = CreditCardRegex.Replace(result, m =>
        {
            string token = $"{{{{CARD_{cardIdx++}}}}}";
            map[token] = m.Value;
            return token;
        });

        // 4. Mask Phone numbers (only if reasonably long to avoid date fragments like 2024-10)
        result = PhoneRegex.Replace(result, m =>
        {
            string digitsOnly = Regex.Replace(m.Value, @"\D", "");
            if (digitsOnly.Length is >= 7 and <= 15)
            {
                string token = $"{{{{PHONE_{phoneIdx++}}}}}";
                map[token] = m.Value;
                return token;
            }
            return m.Value;
        });

        return new PiiMaskResult(result, map, map.Count);
    }

    public string Unmask(string maskedText, IReadOnlyDictionary<string, string> replacementMap)
    {
        if (string.IsNullOrEmpty(maskedText) || replacementMap.Count == 0)
        {
            return maskedText ?? string.Empty;
        }

        string result = maskedText;
        foreach (var (token, originalValue) in replacementMap)
        {
            result = result.Replace(token, originalValue);
        }

        return result;
    }
}
