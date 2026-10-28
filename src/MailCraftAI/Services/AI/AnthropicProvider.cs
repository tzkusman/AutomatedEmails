using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.AI;

public class AnthropicProvider : IAiProvider
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(90) };
    private const string ApiEndpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    public AiProviderType ProviderType => AiProviderType.AnthropicClaude;
    public string DisplayName => "Anthropic Claude";

    public IReadOnlyList<AiModelInfo> AvailableModels { get; } =
    [
        new() { ModelId = "claude-3-7-sonnet-20250219", DisplayName = "Claude 3.7 Sonnet (Latest)", Description = "Highest intelligence with hybrid reasoning", IsRecommended = true },
        new() { ModelId = "claude-3-5-sonnet-20241022", DisplayName = "Claude 3.5 Sonnet", Description = "Proven industry benchmark for nuance & style", IsRecommended = true },
        new() { ModelId = "claude-3-5-haiku-20241022", DisplayName = "Claude 3.5 Haiku", Description = "Ultra-fast, concise, and cost-effective" },
        new() { ModelId = "claude-3-opus-20240229", DisplayName = "Claude 3 Opus", Description = "In-depth writing for complex business communication" }
    ];

    public async IAsyncEnumerable<string> StreamReplyAsync(
        AiGenerationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new InvalidOperationException("Anthropic API key is not configured.");
        }

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(request.ModelId) ? "claude-3-7-sonnet-20250219" : request.ModelId,
            max_tokens = request.MaxTokens > 0 ? request.MaxTokens : 2048,
            temperature = request.Temperature,
            system = request.SystemPrompt ?? string.Empty,
            messages = new[]
            {
                new { role = "user", content = request.UserPrompt }
            },
            stream = true
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint);
        httpRequest.Headers.Add("x-api-key", request.ApiKey.Trim());
        httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Anthropic API returned {(int)response.StatusCode} ({response.ReasonPhrase}): {errorBody}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                string json = line[6..].Trim();
                if (json == "[DONE]") break;

                string? chunk = null;
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "content_block_delta")
                    {
                        if (root.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("text", out var textProp))
                        {
                            chunk = textProp.GetString();
                        }
                    }
                }
                catch
                {
                    // Ignore transient malformed SSE chunks
                }

                if (!string.IsNullOrEmpty(chunk))
                {
                    yield return chunk;
                }
            }
        }
    }

    public async Task<AiGenerationResult> GenerateReplyAsync(
        AiGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sb = new StringBuilder();
            await foreach (var chunk in StreamReplyAsync(request, cancellationToken))
            {
                sb.Append(chunk);
            }

            return new AiGenerationResult
            {
                Success = true,
                Content = sb.ToString()
            };
        }
        catch (Exception ex)
        {
            return new AiGenerationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> ValidateApiKeyAsync(
        string apiKey,
        string model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return false;

        try
        {
            var testRequest = new AiGenerationRequest
            {
                ApiKey = apiKey,
                ModelId = string.IsNullOrWhiteSpace(model) ? "claude-3-5-haiku-20241022" : model,
                SystemPrompt = "Respond with one word: OK",
                UserPrompt = "Ping",
                MaxTokens = 5
            };

            var result = await GenerateReplyAsync(testRequest, cancellationToken);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }
}
