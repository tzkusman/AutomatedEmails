using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.Settings;

public interface ISettingsService
{
    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}
