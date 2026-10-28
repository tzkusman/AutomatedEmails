using System.Collections.Generic;

namespace MailCraftAI.Models;

public class AiGenerationRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? CustomEndpoint { get; set; }
    public double Temperature { get; set; } = 0.5;
    public int MaxTokens { get; set; } = 2048;
}

public class AiGenerationResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? SuggestedSubject { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
}
