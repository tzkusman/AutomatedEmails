using MailCraftAI.Models;
using MailCraftAI.Services.Prompt;
using Xunit;

namespace MailCraftAI.Tests;

public class PromptBuilderServiceTests
{
    private readonly PromptBuilderService _sut = new();

    [Fact]
    public void BuildGenerationPrompts_IncludesCoreSafetyRules()
    {
        var context = new EmailContext
        {
            Subject = "Inquiry regarding pricing",
            SenderName = "Client John",
            SenderEmail = "john@client.com",
            BodyText = "Could you please send the quotation for 50 licenses?"
        };

        var options = new GenerationOptions
        {
            Tone = ToneOption.FriendlyProfessional,
            ReplyLength = ReplyLengthOption.Medium,
            CustomInstruction = "Provide 10% discount and confirm delivery by Friday"
        };

        var profile = new UserBusinessProfile
        {
            FullName = "Jane Doe",
            JobTitle = "Sales Manager",
            CompanyName = "TechSolutions Inc.",
            DefaultSignOff = "Kind regards,"
        };

        var persona = PersonaTemplate.CreateSalesQuotation();

        var prompts = _sut.BuildGenerationPrompts(context, options, profile, persona);

        // Verify System Prompt constraints
        Assert.Contains("CRITICAL RULES", prompts.SystemPrompt);
        Assert.Contains("NEVER invent", prompts.SystemPrompt);
        Assert.Contains("TechSolutions Inc.", prompts.SystemPrompt);
        Assert.Contains("Jane Doe", prompts.SystemPrompt);
        Assert.Contains("Kind regards,", prompts.SystemPrompt);
        Assert.Contains("Sales & Quotation", prompts.SystemPrompt);

        // Verify User Prompt structure
        Assert.Contains("=== INCOMING EMAIL CONTEXT ===", prompts.UserPrompt);
        Assert.Contains("Inquiry regarding pricing", prompts.UserPrompt);
        Assert.Contains("Client John", prompts.UserPrompt);
        Assert.Contains("50 licenses", prompts.UserPrompt);
        Assert.Contains("Provide 10% discount and confirm delivery by Friday", prompts.UserPrompt);
    }

    [Theory]
    [InlineData(QuickActionType.Shorten, "Condense the email")]
    [InlineData(QuickActionType.MakeMoreFormal, "executive corporate tone")]
    [InlineData(QuickActionType.MakeFriendlier, "warmer, more approachable")]
    [InlineData(QuickActionType.FixGrammar, "grammatical mistakes")]
    public void BuildQuickActionPrompts_InjectsAppropriateActionDirectives(QuickActionType action, string expectedPhrase)
    {
        string draft = "Hey team, we need the files by tomorrow. Thanks.";
        var profile = new UserBusinessProfile { FullName = "Jane Doe", DefaultSignOff = "Best regards," };

        var prompts = _sut.BuildQuickActionPrompts(draft, action, "English", profile);

        Assert.Contains(expectedPhrase, prompts.SystemPrompt);
        Assert.Contains(draft, prompts.UserPrompt);
        Assert.Contains("Jane Doe", prompts.SystemPrompt);
    }
}
