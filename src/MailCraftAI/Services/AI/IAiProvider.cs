using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.AI;

public interface IAiProvider
{
    AiProviderType ProviderType { get; }
    string DisplayName { get; }
    IReadOnlyList<AiModelInfo> AvailableModels { get; }

    IAsyncEnumerable<string> StreamReplyAsync(
        AiGenerationRequest request,
        CancellationToken cancellationToken = default);

    Task<AiGenerationResult> GenerateReplyAsync(
        AiGenerationRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateApiKeyAsync(
        string apiKey,
        string model,
        CancellationToken cancellationToken = default);
}
