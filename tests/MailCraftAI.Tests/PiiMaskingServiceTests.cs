using System.Collections.Generic;
using MailCraftAI.Services.Security;
using Xunit;

namespace MailCraftAI.Tests;

public class PiiMaskingServiceTests
{
    private readonly PiiMaskingService _sut = new();

    [Fact]
    public void Mask_WithEmailAddresses_ReplacesWithTokensAndUnmasksCorrectly()
    {
        string input = "Please send the report to alice.smith@enterprise.org and cc bob@work.co.uk.";

        var maskResult = _sut.Mask(input);

        Assert.Contains("{{EMAIL_1}}", maskResult.MaskedText);
        Assert.Contains("{{EMAIL_2}}", maskResult.MaskedText);
        Assert.DoesNotContain("alice.smith@enterprise.org", maskResult.MaskedText);
        Assert.DoesNotContain("bob@work.co.uk", maskResult.MaskedText);
        Assert.Equal(2, maskResult.ItemsMaskedCount);

        string restored = _sut.Unmask(maskResult.MaskedText, maskResult.ReplacementMap);
        Assert.Equal(input, restored);
    }

    [Fact]
    public void Mask_WithPhoneNumbers_ReplacesAndUnmasks()
    {
        string input = "You can call my mobile at +1-555-321-9876 or office 020-7946-0123.";

        var maskResult = _sut.Mask(input);

        Assert.Contains("{{PHONE_1}}", maskResult.MaskedText);
        Assert.DoesNotContain("+1-555-321-9876", maskResult.MaskedText);

        string restored = _sut.Unmask(maskResult.MaskedText, maskResult.ReplacementMap);
        Assert.Equal(input, restored);
    }

    [Fact]
    public void Mask_WithIbanAndCard_ReplacesAndUnmasks()
    {
        string input = "Wire the payment to IBAN DE89370400440532013000 or card 4532-1234-5678-9012.";

        var maskResult = _sut.Mask(input);

        Assert.Contains("{{IBAN_1}}", maskResult.MaskedText);
        Assert.Contains("{{CARD_1}}", maskResult.MaskedText);
        Assert.DoesNotContain("DE89370400440532013000", maskResult.MaskedText);
        Assert.DoesNotContain("4532-1234-5678-9012", maskResult.MaskedText);

        string restored = _sut.Unmask(maskResult.MaskedText, maskResult.ReplacementMap);
        Assert.Equal(input, restored);
    }

    [Fact]
    public void Mask_WithNoPii_LeavesTextIntact()
    {
        string input = "Hello team, let us meet next Monday morning at 10 AM to discuss Q3 targets.";

        var maskResult = _sut.Mask(input);

        Assert.Equal(input, maskResult.MaskedText);
        Assert.Equal(0, maskResult.ItemsMaskedCount);
    }

    [Fact]
    public void Mask_WithEmptyString_ReturnsEmptyResult()
    {
        var maskResult = _sut.Mask("");

        Assert.Equal(string.Empty, maskResult.MaskedText);
        Assert.Equal(0, maskResult.ItemsMaskedCount);
    }
}
