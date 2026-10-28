using System.Threading.Tasks;
using MailCraftAI.Models;

namespace MailCraftAI.Services.Outlook;

public record OutlookStatusResult(
    OutlookStatusType Status,
    string Message,
    bool CanConnect
);

public interface IOutlookService
{
    Task<OutlookStatusResult> CheckStatusAsync();
    Task<EmailContext?> GetCurrentSelectedEmailAsync();
    Task<bool> CreateReplyDraftAsync(EmailContext originalContext, string replyHtml, bool replyAll);
    Task<bool> CreateNewEmailDraftAsync(string subject, string replyHtml, string? toRecipient = null);
    Task<bool> EnsureOutlookRunningAsync();
}
