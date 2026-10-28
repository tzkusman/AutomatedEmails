using MailCraftAI.Services.Context;
using Xunit;

namespace MailCraftAI.Tests;

public class ContextTrimmerServiceTests
{
    private readonly ContextTrimmerService _sut = new();

    [Fact]
    public void ProcessContext_WhenUnderThreshold_DoesNotTrim()
    {
        string text = "Short email message body under limit.";

        var result = _sut.ProcessContext(text, maxCharThreshold: 1000, autoTrim: true);

        Assert.False(result.IsTrimmed);
        Assert.Equal(text, result.ProcessedText);
        Assert.Null(result.WarningMessage);
    }

    [Fact]
    public void ProcessContext_WhenOverThresholdWithThreadDivider_TrimsOldThread()
    {
        string latest = "Please approve the revised invoice.";
        string divider = "-----Original Message-----";
        string oldHistory = new string('A', 3000);
        string fullContext = $"{latest}\n\n{divider}\nFrom: Old Sender\n{oldHistory}";

        var result = _sut.ProcessContext(fullContext, maxCharThreshold: 500, autoTrim: true);

        Assert.True(result.IsTrimmed);
        Assert.Contains(latest, result.ProcessedText);
        Assert.Contains("truncated for brevity", result.ProcessedText);
        Assert.NotNull(result.WarningMessage);
    }

    [Fact]
    public void ProcessContext_WhenAutoTrimDisabled_WarnsWithoutAlteringText()
    {
        string longText = new string('X', 2000);

        var result = _sut.ProcessContext(longText, maxCharThreshold: 500, autoTrim: false);

        Assert.False(result.IsTrimmed);
        Assert.Equal(longText, result.ProcessedText);
        Assert.NotNull(result.WarningMessage);
    }
}
