using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.Security;

public interface ISecureStorageService
{
    Task SaveApiKeyAsync(AiProviderType provider, string apiKey);
    Task<string?> GetApiKeyAsync(AiProviderType provider);
    Task DeleteApiKeyAsync(AiProviderType provider);
    Task<bool> HasApiKeyAsync(AiProviderType provider);
}
