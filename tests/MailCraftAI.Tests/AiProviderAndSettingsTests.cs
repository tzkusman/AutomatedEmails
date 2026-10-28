using System.Linq;
using System.Threading.Tasks;
using MailCraftAI.Models;
using MailCraftAI.Services.AI;
using MailCraftAI.Services.Settings;
using MailCraftAI.Services.Security;
using MailCraftAI.ViewModels;
using Xunit;

namespace MailCraftAI.Tests;

public class AiProviderAndSettingsTests
{
    [Fact]
    public void AnthropicProvider_IncludesClaude37Sonnet_AsRecommended()
    {
        var provider = new AnthropicProvider();
        Assert.Equal(AiProviderType.AnthropicClaude, provider.ProviderType);
        
        var sonnet37 = provider.AvailableModels.FirstOrDefault(m => m.ModelId == "claude-3-7-sonnet-20250219");
        Assert.NotNull(sonnet37);
        Assert.True(sonnet37.IsRecommended);
    }

    [Fact]
    public void OpenAiProvider_IncludesLatestReasoningAndOmniModels()
    {
        var provider = new OpenAiProvider();
        Assert.Equal(AiProviderType.OpenAi, provider.ProviderType);

        var modelIds = provider.AvailableModels.Select(m => m.ModelId).ToList();
        Assert.Contains("gpt-4o", modelIds);
        Assert.Contains("gpt-4o-mini", modelIds);
        Assert.Contains("o3-mini", modelIds);
        Assert.Contains("o1", modelIds);
        Assert.Contains("o1-mini", modelIds);
    }

    [Fact]
    public void GeminiProvider_IncludesGemini38FlashAndNormalizesAliases()
    {
        var provider = new GeminiProvider();
        Assert.Equal(AiProviderType.GoogleGemini, provider.ProviderType);

        var modelIds = provider.AvailableModels.Select(m => m.ModelId).ToList();
        Assert.Contains("gemini-3.8-flash", modelIds);
        Assert.Contains("gemini-3.8-flash-lite", modelIds);
        Assert.Contains("gemini-3.1-pro-preview", modelIds);
        Assert.Contains("gemini-2.5-flash", modelIds);
        Assert.Contains("gemini-2.5-pro", modelIds);

        // Verify deprecated aliases are gracefully normalized to active models
        Assert.Equal("gemini-2.5-flash", GeminiProvider.NormalizeModelId("gemini-2.0-flash"));
        Assert.Equal("gemini-2.5-flash", GeminiProvider.NormalizeModelId("gemini-1.5-flash"));
        Assert.Equal("gemini-2.5-pro", GeminiProvider.NormalizeModelId("gemini-1.5-pro"));
        Assert.Equal("gemini-2.5-flash", GeminiProvider.NormalizeModelId(null));
    }

    [Fact]
    public void SettingsViewModel_PopulatesAllProviderModels()
    {
        var settingsService = new SettingsService();
        var secureStorage = new DpapiSecureStorageService();
        var factory = new AiProviderFactory(new IAiProvider[]
        {
            new AnthropicProvider(),
            new OpenAiProvider(),
            new GeminiProvider()
        });

        var vm = new SettingsViewModel(settingsService, secureStorage, factory);

        Assert.NotEmpty(vm.ClaudeModels);
        Assert.NotEmpty(vm.OpenAiModels);
        Assert.NotEmpty(vm.GeminiModels);
        
        // Assert RelayCommand properties exist and are not null
        Assert.NotNull(vm.SaveCommand);
        Assert.NotNull(vm.ValidateCurrentKeyCommand);
        Assert.NotNull(vm.AddPersonaCommand);
    }
}
