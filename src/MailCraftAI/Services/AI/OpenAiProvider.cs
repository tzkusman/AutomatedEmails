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

public class OpenAiProvider : IAiProvider
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(90) };
    private const string DefaultEndpoint = "https://api.openai.com/v1/chat/completions";

    public AiProviderType ProviderType => AiProviderType.OpenAi;
    public string DisplayName => "OpenAI";

    public IReadOnlyList<AiModelInfo> AvailableModels { get; } =
    [
        new() { ModelId = "gpt-4o", DisplayName = "GPT-4o (Omni Flagship)", Description = "Highest intelligence for nuanced professional replies", IsRecommended = true },
        new() { ModelId = "gpt-4o-mini", DisplayName = "GPT-4o Mini", Description = "Ultra-fast, cost-effective daily email assistant", IsRecommended = true },
        new() { ModelId = "o3-mini", DisplayName = "o3-mini (Reasoning)", Description = "Latest 2025 deep reasoning model for intricate corporate matters" },
        new() { ModelId = "o1", DisplayName = "o1 (Deep Reasoning)", Description = "Advanced reasoning for complex multi-party negotiations" },
        new() { ModelId = "o1-mini", DisplayName = "o1-mini", Description = "Fast reasoning model for technical & analytical responses" },
        new() { ModelId = "gpt-4-turbo", DisplayName = "GPT-4 Turbo", Description = "High-accuracy production model" }
    ];

    public async IAsyncEnumerable<string> StreamReplyAsync(
        AiGenerationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        string endpoint = !string.IsNullOrWhiteSpace(request.CustomEndpoint)
            ? request.CustomEndpoint.Trim()
            : DefaultEndpoint;

        string targetModel = string.IsNullOrWhiteSpace(request.ModelId) ? "gpt-4o" : request.ModelId;
        bool isReasoningModel = targetModel.StartsWith("o1", StringComparison.OrdinalIgnoreCase) ||
                                targetModel.StartsWith("o3", StringComparison.OrdinalIgnoreCase);

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            // Reasoning models (o1/o3) use 'developer' role or prepend to user
            string systemRole = isReasoningModel ? "developer" : "system";
            messages.Add(new { role = systemRole, content = request.SystemPrompt });
        }
        messages.Add(new { role = "user", content = request.UserPrompt });

        var payloadDict = new Dictionary<string, object>
        {
            ["model"] = targetModel,
            ["messages"] = messages,
            ["stream"] = true
        };

        if (isReasoningModel)
        {
            payloadDict["max_completion_tokens"] = request.MaxTokens > 0 ? request.MaxTokens : 2048;
        }
        else
        {
            payloadDict["temperature"] = request.Temperature;
            payloadDict["max_tokens"] = request.MaxTokens > 0 ? request.MaxTokens : 2048;
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey.Trim());
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payloadDict), Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"OpenAI API returned {(int)response.StatusCode} ({response.ReasonPhrase}): {errorBody}");
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
                    var choices = doc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentProp))
                        {
                            chunk = contentProp.GetString();
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
                ModelId = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model,
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
