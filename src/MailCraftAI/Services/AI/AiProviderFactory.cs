using System;
using System.Collections.Generic;
using System.Linq;
using MailCraftAI.Models;

namespace MailCraftAI.Services.AI;

public interface IAiProviderFactory
{
    IAiProvider GetProvider(AiProviderType providerType);
    IEnumerable<IAiProvider> GetAllProviders();
}

public class AiProviderFactory : IAiProviderFactory
{
    private readonly Dictionary<AiProviderType, IAiProvider> _providers;

    public AiProviderFactory(IEnumerable<IAiProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderType);
    }

    public IAiProvider GetProvider(AiProviderType providerType)
    {
        if (_providers.TryGetValue(providerType, out var provider))
        {
            return provider;
        }

        throw new NotSupportedException($"AI provider '{providerType}' is not registered.");
    }

    public IEnumerable<IAiProvider> GetAllProviders() => _providers.Values;
}
