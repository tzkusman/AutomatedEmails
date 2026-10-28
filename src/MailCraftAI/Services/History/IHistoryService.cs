using System.Collections.Generic;
using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.History;

public interface IHistoryService
{
    Task InitializeAsync();
    Task AddRecordAsync(HistoryRecord record);
    Task<IReadOnlyList<HistoryRecord>> GetRecentRecordsAsync(int maxCount = 100, string? searchKeyword = null);
    Task ToggleFavoriteAsync(string recordId);
    Task DeleteRecordAsync(string recordId);
    Task ClearHistoryAsync();
}
