using System;
using System.IO;
using System.Threading.Tasks;
using MailCraftAI.Models;
using MailCraftAI.Services.Security;
using Xunit;

namespace MailCraftAI.Tests;

public class DpapiSecureStorageTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly DpapiSecureStorageService _sut;

    public DpapiSecureStorageTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"MailCraftAI_Test_{Guid.NewGuid():N}.dat");
        _sut = new DpapiSecureStorageService(_tempFilePath);
    }

    [Fact]
    public async Task SaveApiKey_AndGetApiKey_RoundtripsSuccessfully()
    {
        string secret = "sk-ant-api03-test-secret-key-12345";

        await _sut.SaveApiKeyAsync(AiProviderType.AnthropicClaude, secret);

        string? retrieved = await _sut.GetApiKeyAsync(AiProviderType.AnthropicClaude);
        Assert.Equal(secret, retrieved);

        bool hasKey = await _sut.HasApiKeyAsync(AiProviderType.AnthropicClaude);
        Assert.True(hasKey);
    }

    [Fact]
    public async Task DeleteApiKey_RemovesKeyFromStorage()
    {
        await _sut.SaveApiKeyAsync(AiProviderType.OpenAi, "sk-openai-key-abc");
        Assert.True(await _sut.HasApiKeyAsync(AiProviderType.OpenAi));

        await _sut.DeleteApiKeyAsync(AiProviderType.OpenAi);
        Assert.False(await _sut.HasApiKeyAsync(AiProviderType.OpenAi));
        Assert.Null(await _sut.GetApiKeyAsync(AiProviderType.OpenAi));
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            try { File.Delete(_tempFilePath); } catch { /* ignore */ }
        }
    }
}
