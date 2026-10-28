using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.AI;

public class GeminiProvider : IAiProvider
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(90) };
    private const string BaseApiUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public AiProviderType ProviderType => AiProviderType.GoogleGemini;
    public string DisplayName => "Google Gemini";

    public IReadOnlyList<AiModelInfo> AvailableModels { get; } =
    [
        new() { ModelId = "gemini-2.5-flash", DisplayName = "Gemini 2.5 Flash (Recommended)", Description = "Fastest, highest reliability drafting with advanced reasoning", IsRecommended = true },
        new() { ModelId = "gemini-2.5-pro", DisplayName = "Gemini 2.5 Pro", Description = "Deep contextual understanding for complex corporate emails", IsRecommended = true },
        new() { ModelId = "gemini-3.8-flash", DisplayName = "Gemini 3.8 Flash", Description = "Latest generation fast reasoning model" },
        new() { ModelId = "gemini-3.8-flash-lite", DisplayName = "Gemini 3.8 Flash-Lite", Description = "Ultra-fast, cost-effective daily email assistant" },
        new() { ModelId = "gemini-3.1-pro-preview", DisplayName = "Gemini 3.1 Pro (Deep Reasoning)", Description = "Highest Google reasoning intelligence for demanding writing tasks" },
        new() { ModelId = "gemini-flash-latest", DisplayName = "Gemini Flash (Latest Auto)", Description = "Always routes to the newest stable Flash model" },
        new() { ModelId = "gemini-pro-latest", DisplayName = "Gemini Pro (Latest Auto)", Description = "Always routes to the newest stable Pro model" }
    ];

    public static string NormalizeModelId(string? modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return "gemini-2.5-flash";
        }

        return modelId.Trim().ToLowerInvariant() switch
        {
            "gemini-2.0-flash" => "gemini-2.5-flash",
            "gemini-2.0-flash-thinking-exp-01-21" => "gemini-2.5-flash",
            "gemini-2.0-pro-exp-02-05" => "gemini-2.5-pro",
            "gemini-1.5-flash" => "gemini-2.5-flash",
            "gemini-1.5-pro" => "gemini-2.5-pro",
            _ => modelId.Trim()
        };
    }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        AiGenerationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new InvalidOperationException("Google Gemini API key is not configured.");
        }

        string model = NormalizeModelId(request.ModelId);
        string url = $"{BaseApiUrl}/{model}:streamGenerateContent?alt=sse&key={Uri.EscapeDataString(request.ApiKey.Trim())}";

        var payload = new Dictionary<string, object>
        {
            ["contents"] = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = request.UserPrompt } }
                }
            },
            ["generationConfig"] = new
            {
                temperature = request.Temperature,
                maxOutputTokens = request.MaxTokens > 0 ? request.MaxTokens : 2048
            }
        };

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            payload["systemInstruction"] = new
            {
                parts = new[] { new { text = request.SystemPrompt } }
            };
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Gemini API returned {(int)response.StatusCode} ({response.ReasonPhrase}): {errorBody}");
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
                    if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                    {
                        var candidate = candidates[0];
                        if (candidate.TryGetProperty("content", out var content) &&
                            content.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0)
                        {
                            var part = parts[0];
                            if (part.TryGetProperty("text", out var textProp))
                            {
                                chunk = textProp.GetString();
                            }
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
                ModelId = NormalizeModelId(model),
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
